using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Sleepover > Setup Stella Room Talk & Dinner Route
// - Stella faces Nora once she stands in her room, and can be talked to there
// - the talk: Stella -> Dad (off-screen) -> Stella "let's go eat"
// - after the talk Stella leads the way back: room -> Corridor 2 -> Corridor 1 -> Kitchen
// Safe to run again: points and dialogue you already edited are kept.
public static class StellaDinnerSetup
{
    private const string DialogueFolder = "Assets/Data/Dialogue/_Main Story/StellasRoomTalk";
    private const string DoorOpeningGuid = "40cd69781a7c80f46b684d069ab090e2";

    [MenuItem("Tools/Sleepover/Setup Stella Room Talk & Dinner Route")]
    public static void Setup()
    {
        var report = new List<string>();

        NpcMovement stella = null;
        foreach (NpcMovement npc in Object.FindObjectsByType<NpcMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (npc.name == "Stella") stella = npc;
        NpcGuide guide = stella != null ? stella.GetComponent<NpcGuide>() : null;
        if (stella == null || guide == null)
        {
            EditorUtility.DisplayDialog("Setup", "Stella or her NpcGuide is missing. Run 'Setup Stella Route' first.", "OK");
            return;
        }

        RoomDoor c2ToStella = FindDoor("Corridor2 TO Stella's room");
        RoomDoor stellaToC2 = FindDoor("Stella's room TO Corridor2");
        RoomDoor c2ToC1 = FindDoor("Corridor2 TO Corridor1");
        RoomDoor c1ToKitchen = FindDoor("Corridor1 TO Kitchen");
        if (c2ToStella == null || stellaToC2 == null || c2ToC1 == null || c1ToKitchen == null)
        {
            EditorUtility.DisplayDialog("Setup", "Couldn't find one of the doors (Stella's room / Corridor2 / Corridor1 / Kitchen).", "OK");
            return;
        }

        // ---------- Dialogue ----------
        DialogueNode d3 = GetOrCreateNode("3_LetsGo", "Stella", "Oh, that's my dad! Come on, let's go eat before it gets cold!", null, report);
        DialogueNode d2 = GetOrCreateNode("2_Dad", "Mr. Lockbell", "Stella, I'm back! Dinner is ready!", d3, report);
        DialogueNode d1 = GetOrCreateNode("1_MyRoom", "Stella", "So... this is my room! Make yourself at home, Nora.", d2, report);

        // ---------- Stella: face Nora, talkable once she's in her room ----------
        var moveSO = new SerializedObject(stella);
        moveSO.FindProperty("lookAtPlayerWhenIdle").boolValue = true;
        moveSO.ApplyModifiedProperties();

        if (stella.GetComponent<Collider>() == null)
        {
            var capsule = Undo.AddComponent<CapsuleCollider>(stella.gameObject);
            capsule.center = new Vector3(0f, 0.5f, 0f);
            capsule.radius = 0.3f;
            capsule.height = 1f;
            report.Add("Added a collider to Stella so she can be talked to.");
        }

        InteractionLock talkLock = stella.GetComponent<InteractionLock>();
        if (talkLock == null) talkLock = Undo.AddComponent<InteractionLock>(stella.gameObject);

        NPC npcTalk = stella.GetComponent<NPC>();
        if (npcTalk == null) npcTalk = Undo.AddComponent<NPC>(stella.gameObject);
        var npcSO = new SerializedObject(npcTalk);
        npcSO.FindProperty("interactionPrompt").stringValue = "Talk";
        npcSO.FindProperty("startingNode").objectReferenceValue = d1;
        npcSO.FindProperty("talkOnlyOnce").boolValue = true;
        npcSO.FindProperty("onDialogueFinished").FindPropertyRelative("m_PersistentCalls.m_Calls").arraySize = 0;
        npcSO.ApplyModifiedProperties();
        UnityEventTools.AddPersistentListener(GetEvent(npcTalk, "onDialogueFinished"), guide.ContinueRoute);
        report.Add("Stella: 'Talk' unlocks when she reaches her spot; after the talk she leads you to the kitchen.");

        // ---------- Points ----------
        Transform roomPoint = FindInScene("StellaPoint_StellasRoom")?.transform;
        Vector3 roomPos = roomPoint != null ? roomPoint.position : c2ToStella.Destination.position;
        Transform exitPoint = GetOrCreatePoint("StellaPoint_StellasRoom_Exit", NearDoor(stellaToC2, roomPos), report);
        Transform c2Point = GetOrCreatePoint("StellaPoint_Corridor2_ToCorridor1", NearDoor(c2ToC1, stellaToC2.Destination.position), report);
        Transform c1Point = GetOrCreatePoint("StellaPoint_Corridor1_ToKitchen", NearDoor(c1ToKitchen, c2ToC1.Destination.position), report);
        Transform kitchenPoint = GetOrCreatePoint("StellaPoint_Kitchen", c1ToKitchen.Destination.position + new Vector3(1.5f, 0f, 1f), report);

        AudioClip doorSound = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(DoorOpeningGuid));

        // ---------- Route: keep steps 1-3, (re)write the way back ----------
        var guideSO = new SerializedObject(guide);
        SerializedProperty steps = guideSO.FindProperty("steps");
        if (steps.arraySize < 3)
        {
            EditorUtility.DisplayDialog("Setup", "Stella's route has fewer than 3 steps. Run 'Setup Stella Route' first.", "OK");
            return;
        }
        steps.arraySize = 7;
        FillStep(steps.GetArrayElementAtIndex(3), "Back: leave Stella's room", 0f, exitPoint, stellaToC2, doorSound);
        FillStep(steps.GetArrayElementAtIndex(4), "Back: Corridor 2", 6f, c2Point, c2ToC1, doorSound);
        FillStep(steps.GetArrayElementAtIndex(5), "Back: Corridor 1", 6f, c1Point, c1ToKitchen, doorSound);
        FillStep(steps.GetArrayElementAtIndex(6), "Kitchen (end)", 6f, kitchenPoint, null, null);
        // Step 3 (Stella's room): unlock talking when she arrives.
        steps.GetArrayElementAtIndex(2).FindPropertyRelative("onArrived.m_PersistentCalls.m_Calls").arraySize = 0;
        guideSO.ApplyModifiedProperties();

        var stepArray = (System.Array)typeof(NpcGuide).GetField("steps", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(guide);
        var roomStep = (NpcGuide.GuideStep)stepArray.GetValue(2);
        UnityEventTools.AddPersistentListener(roomStep.onArrived, talkLock.Unlock);
        EditorUtility.SetDirty(guide);
        report.Add("Route back: Stella's room -> Corridor 2 -> Corridor 1 -> Kitchen.");

        EditorSceneManager.MarkSceneDirty(stella.gameObject.scene);
        EditorSceneManager.SaveScene(stella.gameObject.scene);
        report.Add("Scene saved.");

        EditorUtility.DisplayDialog("Setup", "Done!\n\n- " + string.Join("\n- ", report), "OK");
    }

    // ---------------- Helpers ----------------

    private static UnityEngine.Events.UnityEvent GetEvent(object target, string field)
    {
        return (UnityEngine.Events.UnityEvent)target.GetType()
            .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    }

    private static DialogueNode GetOrCreateNode(string name, string speaker, string text, DialogueNode next, List<string> report)
    {
        if (!AssetDatabase.IsValidFolder(DialogueFolder))
        {
            AssetDatabase.CreateFolder("Assets/Data/Dialogue/_Main Story", "StellasRoomTalk");
        }

        string path = DialogueFolder + "/" + name + ".asset";
        DialogueNode node = AssetDatabase.LoadAssetAtPath<DialogueNode>(path);
        if (node != null)
            return node;   // keep the user's edits

        node = ScriptableObject.CreateInstance<DialogueNode>();
        node.speaker = speaker;
        node.text = text;
        node.nextNode = next;
        AssetDatabase.CreateAsset(node, path);
        report.Add("Created dialogue " + name);
        return node;
    }

    private static void FillStep(SerializedProperty step, string note, float waitForPlayer, Transform walkTo, RoomDoor door, AudioClip sound)
    {
        step.FindPropertyRelative("note").stringValue = note;
        step.FindPropertyRelative("waitForPlayerWithin").floatValue = waitForPlayer;
        step.FindPropertyRelative("waitBeforeWalking").floatValue = 0.5f;
        step.FindPropertyRelative("walkTo").objectReferenceValue = walkTo;
        step.FindPropertyRelative("door").objectReferenceValue = door;
        step.FindPropertyRelative("waitBeforeDoor").floatValue = 1f;
        step.FindPropertyRelative("doorSound").objectReferenceValue = sound;
        step.FindPropertyRelative("onArrived.m_PersistentCalls.m_Calls").arraySize = 0;
    }

    private static Vector3 NearDoor(RoomDoor door, Vector3 comingFrom)
    {
        Vector3 doorPos = door.transform.position;
        Vector3 toward = comingFrom - doorPos;
        toward.y = 0f;
        Vector3 point = doorPos + (toward.sqrMagnitude > 0.01f ? toward.normalized : Vector3.back);
        point.y = comingFrom.y;
        return point;
    }

    private static Transform GetOrCreatePoint(string name, Vector3 position, List<string> report)
    {
        GameObject existing = FindInScene(name);
        if (existing != null)
            return existing.transform;

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create point");
        go.transform.position = position;
        report.Add("Created '" + name + "' (check its position).");
        return go.transform;
    }

    private static GameObject FindInScene(string name)
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == name && t.gameObject.scene.IsValid())
                return t.gameObject;
        return null;
    }

    private static RoomDoor FindDoor(string doorObjectName)
    {
        foreach (RoomDoor door in Object.FindObjectsByType<RoomDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            for (Transform t = door.transform; t != null; t = t.parent)
                if (t.name == doorObjectName)
                    return door;
        }
        return null;
    }
}
