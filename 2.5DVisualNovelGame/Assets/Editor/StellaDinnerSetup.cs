using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Sleepover > Setup Stella -> Kitchen Route
// After Stella comes back and you answer YES to her "ready to eat?" question,
// she leads the way: her room -> Corridor 2 -> Corridor 1 -> Kitchen.
// Her room's door only opens once she has gone through it.
// Safe to run again: points you moved are kept.
public static class StellaDinnerSetup
{
    private const string DoorOpeningGuid = "40cd69781a7c80f46b684d069ab090e2";

    [MenuItem("Tools/Sleepover/Setup Stella -> Kitchen Route")]
    public static void Setup()
    {
        var report = new List<string>();

        NpcMovement stella = null;
        foreach (NpcMovement npc in Object.FindObjectsByType<NpcMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (npc.name == "Stella") stella = npc;
        NpcGuide guide = stella != null ? stella.GetComponent<NpcGuide>() : null;
        NpcLeaveAndReturn sequence = Object.FindFirstObjectByType<NpcLeaveAndReturn>(FindObjectsInactive.Include);
        if (stella == null || guide == null || sequence == null)
        {
            EditorUtility.DisplayDialog("Setup", "Need Stella with her NpcGuide, and 'Stella Leaves & Returns'. Run those setups first.", "OK");
            return;
        }

        RoomDoor stellaToC2 = FindDoor("Stella's room TO Corridor2");
        RoomDoor c2ToC1 = FindDoor("Corridor2 TO Corridor1");
        RoomDoor c1ToKitchen = FindDoor("Corridor1 TO Kitchen");
        if (stellaToC2 == null || c2ToC1 == null || c1ToKitchen == null ||
            stellaToC2.Destination == null || c2ToC1.Destination == null || c1ToKitchen.Destination == null)
        {
            EditorUtility.DisplayDialog("Setup", "Couldn't find the doors 'Stella's room TO Corridor2', 'Corridor2 TO Corridor1', 'Corridor1 TO Kitchen' (with destinations).", "OK");
            return;
        }

        // ---------- Points ----------
        Transform exitPoint = GetOrCreatePoint("StellaPoint_StellasRoom_Exit", stellaToC2.transform.position + Vector3.back, report);
        Transform c2Point = GetOrCreatePoint("StellaPoint_Corridor2_ToCorridor1", NearDoor(c2ToC1, stellaToC2.Destination.position), report);
        Transform c1Point = GetOrCreatePoint("StellaPoint_Corridor1_ToKitchen", NearDoor(c1ToKitchen, c2ToC1.Destination.position), report);
        Transform kitchenPoint = GetOrCreatePoint("StellaPoint_Kitchen", c1ToKitchen.Destination.position + new Vector3(1.5f, 0f, 1f), report);

        AudioClip doorSound = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(DoorOpeningGuid));

        // ---------- Route: keep steps 1-3 (to her room), add the way to the kitchen ----------
        var guideSO = new SerializedObject(guide);
        SerializedProperty steps = guideSO.FindProperty("steps");
        if (steps.arraySize < 3)
        {
            EditorUtility.DisplayDialog("Setup", "Stella's route has fewer than 3 steps. Run 'Setup Stella Route' first.", "OK");
            return;
        }
        steps.arraySize = 7;
        FillStep(steps.GetArrayElementAtIndex(3), "Kitchen 1: leave Stella's room", 0f, exitPoint, stellaToC2, doorSound);
        FillStep(steps.GetArrayElementAtIndex(4), "Kitchen 2: Corridor 2", 6f, c2Point, c2ToC1, doorSound);
        FillStep(steps.GetArrayElementAtIndex(5), "Kitchen 3: Corridor 1", 6f, c1Point, c1ToKitchen, doorSound);
        FillStep(steps.GetArrayElementAtIndex(6), "Kitchen (end)", 6f, kitchenPoint, null, null);
        guideSO.ApplyModifiedProperties();
        report.Add("Route after YES: Stella's room -> Corridor 2 -> Corridor 1 -> Kitchen.");

        // ---------- Sequence: YES starts the route ----------
        var seqSO = new SerializedObject(sequence);
        seqSO.FindProperty("routeAfterYes").objectReferenceValue = guide;
        seqSO.ApplyModifiedProperties();
        if (seqSO.FindProperty("readyQuestion").objectReferenceValue == null)
            report.Add("TO DO: drag your 'Are you ready to go eat now?' line into 'Ready Question' (selected object).");

        Selection.activeGameObject = sequence.gameObject;
        EditorSceneManager.MarkSceneDirty(stella.gameObject.scene);
        EditorSceneManager.SaveScene(stella.gameObject.scene);
        report.Add("Scene saved.");

        EditorUtility.DisplayDialog("Setup", "Done!\n\n- " + string.Join("\n- ", report), "OK");
    }

    // ---------------- Helpers ----------------

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
