using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Sleepover > Setup Stella Route
// Stella leads the way: Living room -> Corridor 1 -> Corridor 2 -> Stella's room.
// Creates one point per room (move them where you like) and fills Stella's NpcGuide.
// Safe to run again: existing points are kept where you moved them.
public static class StellaRouteSetup
{
    private const string DoorOpeningGuid = "40cd69781a7c80f46b684d069ab090e2";

    [MenuItem("Tools/Sleepover/Setup Stella Route")]
    public static void Setup()
    {
        var report = new List<string>();

        NpcMovement stella = null;
        foreach (NpcMovement npc in Object.FindObjectsByType<NpcMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (npc.name == "Stella") stella = npc;

        if (stella == null)
        {
            EditorUtility.DisplayDialog("Setup Stella Route", "No 'Stella' found. Run 'Setup Stella & Corridor Door' first.", "OK");
            return;
        }

        RoomDoor livingToC1 = FindDoor("Living room TO Corridor1");
        RoomDoor c1ToC2 = FindDoor("Corridor1 TO Corridor2");
        RoomDoor c2ToStella = FindDoor("Corridor2 TO Stella's room");

        if (livingToC1 == null || c1ToC2 == null || c2ToStella == null ||
            livingToC1.Destination == null || c1ToC2.Destination == null || c2ToStella.Destination == null)
        {
            EditorUtility.DisplayDialog("Setup Stella Route",
                "Couldn't find the 3 doors (Living room TO Corridor1, Corridor1 TO Corridor2, Corridor2 TO Stella's room) with destinations.", "OK");
            return;
        }

        // One point per room: next to the door Stella will take (1 unit toward where she arrives).
        Transform p1 = GetOrCreatePoint("StellaPoint_Corridor1", NearDoor(c1ToC2, livingToC1.Destination.position), report);
        Transform p2 = GetOrCreatePoint("StellaPoint_Corridor2", NearDoor(c2ToStella, c1ToC2.Destination.position), report);
        Transform p3 = GetOrCreatePoint("StellaPoint_StellasRoom", c2ToStella.Destination.position + new Vector3(1.5f, 0f, 1f), report);

        AudioClip doorSound = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(DoorOpeningGuid));

        NpcGuide guide = stella.GetComponent<NpcGuide>();
        if (guide == null) guide = Undo.AddComponent<NpcGuide>(stella.gameObject);

        var so = new SerializedObject(guide);
        SerializedProperty steps = so.FindProperty("steps");
        steps.arraySize = 3;
        FillStep(steps.GetArrayElementAtIndex(0), "Corridor 1", p1, c1ToC2, doorSound);
        FillStep(steps.GetArrayElementAtIndex(1), "Corridor 2", p2, c2ToStella, doorSound);
        FillStep(steps.GetArrayElementAtIndex(2), "Stella's room (end)", p3, null, null);
        so.ApplyModifiedProperties();
        report.Add("Stella's route: Corridor 1 -> Corridor 2 -> Stella's room.");

        // The old test trigger moved/teleported Stella too: switch it off so they don't fight.
        GameObject oldTrigger = FindInScene("Trigger_Corridor1");
        if (oldTrigger != null && oldTrigger.activeSelf)
        {
            Undo.RecordObject(oldTrigger, "Disable old trigger");
            oldTrigger.SetActive(false);
            report.Add("Switched OFF 'Trigger_Corridor1' (it fought with the route). Re-tick it if you still need it.");
        }

        EditorSceneManager.MarkSceneDirty(stella.gameObject.scene);
        EditorSceneManager.SaveScene(stella.gameObject.scene);
        report.Add("Scene saved.");

        EditorUtility.DisplayDialog("Setup Stella Route", "Done!\n\n- " + string.Join("\n- ", report), "OK");
    }

    private static void FillStep(SerializedProperty step, string note, Transform walkTo, RoomDoor door, AudioClip sound)
    {
        step.FindPropertyRelative("note").stringValue = note;
        step.FindPropertyRelative("waitForPlayerWithin").floatValue = 6f;
        step.FindPropertyRelative("waitBeforeWalking").floatValue = 0.5f;
        step.FindPropertyRelative("walkTo").objectReferenceValue = walkTo;
        step.FindPropertyRelative("door").objectReferenceValue = door;
        step.FindPropertyRelative("waitBeforeDoor").floatValue = 1f;
        step.FindPropertyRelative("doorSound").objectReferenceValue = sound;
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
        report.Add("Created '" + name + "' (move it where Stella should stand).");
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
