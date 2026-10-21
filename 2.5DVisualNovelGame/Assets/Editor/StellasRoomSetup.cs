using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Sleepover > Setup Stella's Room
//   Stella's talk uses StellasRoom/1-4, she stays in her room, her door says "No need to leave now."
// Tools > Sleepover > Make Selected Objects Inspectable
//   Select any objects (bed, TV...) -> they get "[E] Inspect" + a dialogue slot to fill.
// Tools > Sleepover > Block Selected Doors (No need to leave now)
//   Select door objects -> pressing E on them shows the message instead of leaving.
public static class StellasRoomSetup
{
    private const string StellasRoomTalkGuid = "06f62caccfcfa49419cbb371f5a91927"; // StellasRoom/1
    private const string NoNeedToLeavePath = "Assets/Data/Dialogue/_Optionals/Doors/NoNeedToLeave.asset";

    [MenuItem("Tools/Sleepover/Setup Stella's Room")]
    public static void Setup()
    {
        var report = new List<string>();

        NpcMovement stella = null;
        foreach (NpcMovement npc in Object.FindObjectsByType<NpcMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (npc.name == "Stella") stella = npc;
        if (stella == null)
        {
            EditorUtility.DisplayDialog("Setup", "No 'Stella' found in the open scene.", "OK");
            return;
        }

        // Stella's talk: StellasRoom 1 -> 2 -> 3 -> 4, and nothing after (she stays).
        NPC npcTalk = stella.GetComponent<NPC>();
        if (npcTalk != null)
        {
            var so = new SerializedObject(npcTalk);
            so.FindProperty("startingNode").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DialogueNode>(AssetDatabase.GUIDToAssetPath(StellasRoomTalkGuid));
            so.FindProperty("onDialogueFinished").FindPropertyRelative("m_PersistentCalls.m_Calls").arraySize = 0;
            so.ApplyModifiedProperties();
            report.Add("Stella's talk now uses StellasRoom/1-4.");
        }

        // Route: keep only the way to her room (no going back to the kitchen).
        NpcGuide guide = stella.GetComponent<NpcGuide>();
        if (guide != null)
        {
            var so = new SerializedObject(guide);
            SerializedProperty steps = so.FindProperty("steps");
            if (steps.arraySize > 3) steps.arraySize = 3;
            so.ApplyModifiedProperties();
            report.Add("Stella stays in her room after the talk.");
        }

        // Her room's door: "No need to leave now."
        RoomDoor door = FindDoor("Stella's room TO Corridor2");
        if (door != null)
        {
            AddBlocker(door.gameObject);
            report.Add("Stella's room door says 'No need to leave now.'");
        }

        Save(stella.gameObject, report);
    }

    [MenuItem("Tools/Sleepover/Make Selected Objects Inspectable")]
    public static void MakeInspectable()
    {
        int count = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            if (go.GetComponent<InspectDialogue>() != null)
                continue;

            if (go.GetComponentInChildren<Collider>() == null)
            {
                BoxCollider box = Undo.AddComponent<BoxCollider>(go);
                FitBoxToRenderers(go, box);
            }

            Undo.AddComponent<InspectDialogue>(go);
            count++;
        }

        if (count > 0) EditorSceneManager.MarkSceneDirty(Selection.activeGameObject.scene);
        EditorUtility.DisplayDialog("Inspectable",
            count + " object(s) made inspectable.\n\nNow click each one and drag its first dialogue line into 'Dialogue' (Inspect Dialogue component).\nThen Ctrl+S.",
            "OK");
    }

    [MenuItem("Tools/Sleepover/Block Selected Doors (No need to leave now)")]
    public static void BlockSelectedDoors()
    {
        int count = 0;
        foreach (GameObject go in Selection.gameObjects)
        {
            RoomDoor roomDoor = go.GetComponentInChildren<RoomDoor>();
            LockedRoomDoor lockedDoor = go.GetComponentInChildren<LockedRoomDoor>();
            GameObject target = roomDoor != null ? roomDoor.gameObject : lockedDoor != null ? lockedDoor.gameObject : null;
            if (target == null) continue;
            AddBlocker(target);
            count++;
        }

        if (count > 0) EditorSceneManager.MarkSceneDirty(Selection.activeGameObject.scene);
        EditorUtility.DisplayDialog("Block doors",
            count + " door(s) now say 'No need to leave now.'\n(Select the door objects or their parents.)\nThen Ctrl+S.", "OK");
    }

    [MenuItem("Tools/Sleepover/Setup Stella Leaves & Returns")]
    public static void SetupLeaveAndReturn()
    {
        var report = new List<string>();

        NpcMovement stella = null;
        foreach (NpcMovement npc in Object.FindObjectsByType<NpcMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (npc.name == "Stella") stella = npc;
        NPC stellaTalk = stella != null ? stella.GetComponent<NPC>() : null;
        RoomDoor exitDoor = FindDoor("Stella's room TO Corridor2");
        RoomDoor intoRoom = FindDoor("Corridor2 TO Stella's room");
        if (stella == null || stellaTalk == null || exitDoor == null || intoRoom == null || intoRoom.Destination == null)
        {
            EditorUtility.DisplayDialog("Setup", "Need Stella (with NPC talk) and the doors 'Stella's room TO Corridor2' / 'Corridor2 TO Stella's room'.", "OK");
            return;
        }

        // Point near the door, inside the room: where she leaves from and waits afterwards.
        Transform doorPoint = null;
        GameObject existing = null;
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == "StellaPoint_StellasRoom_Exit") existing = t.gameObject;
        if (existing == null)
        {
            existing = new GameObject("StellaPoint_StellasRoom_Exit");
            Undo.RegisterCreatedObjectUndo(existing, "Create point");
            Vector3 doorPos = exitDoor.transform.position;
            Vector3 toward = intoRoom.Destination.position - doorPos; toward.y = 0f;
            existing.transform.position = doorPos + (toward.sqrMagnitude > 0.01f ? toward.normalized : Vector3.back) * 1.2f;
            report.Add("Created 'StellaPoint_StellasRoom_Exit' near her door (check it).");
        }
        doorPoint = existing.transform;

        // The sequence object.
        NpcLeaveAndReturn sequence = Object.FindFirstObjectByType<NpcLeaveAndReturn>(FindObjectsInactive.Include);
        if (sequence == null)
        {
            var go = new GameObject("Stella Leaves & Returns");
            Undo.RegisterCreatedObjectUndo(go, "Create sequence");
            sequence = go.AddComponent<NpcLeaveAndReturn>();
        }

        var so = new SerializedObject(sequence);
        so.FindProperty("npc").objectReferenceValue = stella;
        so.FindProperty("leavePoint").objectReferenceValue = doorPoint;
        so.FindProperty("exitDoor").objectReferenceValue = exitDoor;
        so.FindProperty("doorSound").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath("40cd69781a7c80f46b684d069ab090e2"));
        so.FindProperty("waitSeconds").floatValue = 15f;
        so.FindProperty("returnSpawn").objectReferenceValue = intoRoom.Destination;
        so.FindProperty("waitPoint").objectReferenceValue = doorPoint;
        so.FindProperty("returnDialogue").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<DialogueNode>(AssetDatabase.GUIDToAssetPath("ac8c5d518191e52478de4d02fc47efe7")); // Let's Dine/1

        BlockedDoor blocker = exitDoor.GetComponent<BlockedDoor>();
        SerializedProperty unblock = so.FindProperty("unblockDoors");
        unblock.arraySize = blocker != null ? 1 : 0;
        if (blocker != null) unblock.GetArrayElementAtIndex(0).objectReferenceValue = blocker;

        // Every inspectable object in Stella's room gets a (new dialogue) slot to fill.
        var inspectables = new List<InspectDialogue>();
        foreach (InspectDialogue inspect in Object.FindObjectsByType<InspectDialogue>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            for (Transform t = inspect.transform; t != null; t = t.parent)
                if (t.name == "StellasRoom") { inspectables.Add(inspect); break; }
        }

        SerializedProperty swaps = so.FindProperty("dialogueSwaps");
        var alreadyListed = new HashSet<Object>();
        for (int i = 0; i < swaps.arraySize; i++)
            alreadyListed.Add(swaps.GetArrayElementAtIndex(i).FindPropertyRelative("target").objectReferenceValue);
        foreach (InspectDialogue inspect in inspectables)
        {
            if (alreadyListed.Contains(inspect)) continue;
            swaps.arraySize++;
            swaps.GetArrayElementAtIndex(swaps.arraySize - 1).FindPropertyRelative("target").objectReferenceValue = inspect;
            swaps.GetArrayElementAtIndex(swaps.arraySize - 1).FindPropertyRelative("newDialogue").objectReferenceValue = null;
        }
        so.ApplyModifiedProperties();
        report.Add(inspectables.Count + " inspectable object(s) in Stella's room listed in 'Dialogue Swaps' (fill their new dialogues).");

        // Start the sequence when Stella's talk is over.
        var npcSO = new SerializedObject(stellaTalk);
        npcSO.FindProperty("onDialogueFinished").FindPropertyRelative("m_PersistentCalls.m_Calls").arraySize = 0;
        npcSO.ApplyModifiedProperties();
        var evt = (UnityEngine.Events.UnityEvent)typeof(NPC)
            .GetField("onDialogueFinished", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(stellaTalk);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(evt, sequence.Begin);
        EditorUtility.SetDirty(stellaTalk);
        report.Add("After Stella's talk: she leaves, comes back after 15s (paused during dialogues), says 'Let's Dine', then waits by the door.");

        Selection.activeGameObject = sequence.gameObject;
        Save(stella.gameObject, report);
    }

    // ---------------- Helpers ----------------

    private static void AddBlocker(GameObject doorObject)
    {
        BlockedDoor blocker = doorObject.GetComponent<BlockedDoor>();
        if (blocker == null) blocker = Undo.AddComponent<BlockedDoor>(doorObject);

        var so = new SerializedObject(blocker);
        if (so.FindProperty("blockedDialogue").objectReferenceValue == null)
            so.FindProperty("blockedDialogue").objectReferenceValue = GetNoNeedToLeaveDialogue();
        so.ApplyModifiedProperties();
    }

    private static DialogueNode GetNoNeedToLeaveDialogue()
    {
        DialogueNode node = AssetDatabase.LoadAssetAtPath<DialogueNode>(NoNeedToLeavePath);
        if (node != null)
            return node;

        if (!AssetDatabase.IsValidFolder("Assets/Data/Dialogue/_Optionals/Doors"))
            AssetDatabase.CreateFolder("Assets/Data/Dialogue/_Optionals", "Doors");

        node = ScriptableObject.CreateInstance<DialogueNode>();
        node.speaker = "Nora";
        node.text = "No need to leave now.";
        AssetDatabase.CreateAsset(node, NoNeedToLeavePath);
        AssetDatabase.SaveAssets();
        return node;
    }

    private static void FitBoxToRenderers(GameObject go, BoxCollider box)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds world = renderers[0].bounds;
        foreach (Renderer r in renderers) world.Encapsulate(r.bounds);

        // Convert world bounds to the object's local space.
        Vector3 localCenter = go.transform.InverseTransformPoint(world.center);
        Vector3 localSize = go.transform.InverseTransformVector(world.size);
        box.center = localCenter;
        box.size = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));
    }

    private static void Save(GameObject any, List<string> report)
    {
        EditorSceneManager.MarkSceneDirty(any.scene);
        EditorSceneManager.SaveScene(any.scene);
        report.Add("Scene saved.");
        EditorUtility.DisplayDialog("Setup", "Done!\n\n- " + string.Join("\n- ", report), "OK");
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
