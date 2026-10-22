using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Tools > Sleepover > Setup Kitchen Scene (Dad)
// - creates "Dad" in the kitchen (placeholder look: Nora's frames, tinted) setting the table
// - Stella waits next to the kitchen door
// - when Nora enters: she can't move, Dad turns to her for a second, then talks
// Safe to run again: Dad and points you moved are kept.
public static class KitchenSetup
{
    private const string DadName = "Dad";
    private const string NoraWalkFolder = "Assets/Art/Characters/Nora/Walk";

    [MenuItem("Tools/Sleepover/Setup Kitchen Scene (Dad)")]
    public static void Setup()
    {
        var report = new List<string>();

        CharacterMovement player = Object.FindFirstObjectByType<CharacterMovement>(FindObjectsInactive.Include);
        NpcMovement stella = null;
        foreach (NpcMovement npc in Object.FindObjectsByType<NpcMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (npc.name == "Stella") stella = npc;
        RoomDoor kitchenDoor = FindDoor("Corridor1 TO Kitchen");
        if (player == null || stella == null || kitchenDoor == null || kitchenDoor.Destination == null)
        {
            EditorUtility.DisplayDialog("Setup", "Need the player, Stella and the door 'Corridor1 TO Kitchen' (with a destination).", "OK");
            return;
        }

        Transform arrival = kitchenDoor.Destination;

        // ---------- The table ----------
        Bounds table = FindKitchenTable(arrival.position, report);
        float floorY = arrival.position.y;

        // ---------- Stella waits next to the door ----------
        GameObject stellaSpot = FindInScene("StellaPoint_Kitchen");
        if (stellaSpot != null)
        {
            Undo.RecordObject(stellaSpot.transform, "Move Stella spot");
            stellaSpot.transform.position = arrival.position + arrival.right * 1.2f;
            report.Add("Moved 'StellaPoint_Kitchen' next to the kitchen door (adjust if needed).");
        }

        // ---------- Dad's points around the table ----------
        Vector3 behind = new Vector3(table.center.x, floorY, table.max.z + 0.4f);
        Transform pointA = GetOrCreatePoint("DadPoint_Table_A", behind + Vector3.left * table.extents.x * 0.6f, report);
        Transform pointB = GetOrCreatePoint("DadPoint_Table_B", behind + Vector3.right * table.extents.x * 0.6f, report);
        Transform lookAt = GetOrCreatePoint("DadLookAt_Table", new Vector3(table.center.x, floorY, table.center.z), report);

        // ---------- Dad ----------
        GameObject dad = FindInScene(DadName);
        if (dad == null)
        {
            dad = new GameObject(DadName);
            Undo.RegisterCreatedObjectUndo(dad, "Create Dad");
            dad.transform.position = pointA.position;

            // Same look setup as Stella (visual child + shadow).
            foreach (Transform child in stella.transform)
            {
                if (child.name != "Visual" && child.name != "Shadows") continue;
                GameObject copy = Object.Instantiate(child.gameObject, dad.transform);
                copy.name = child.name;
                copy.transform.localPosition = child.localPosition;
                copy.transform.localRotation = child.localRotation;
                copy.transform.localScale = child.localScale;
            }
            report.Add("Created 'Dad' behind the kitchen table.");
        }

        Transform visual = dad.transform.Find("Visual");
        SpriteRenderer renderer = visual != null ? visual.GetComponent<SpriteRenderer>() : dad.GetComponentInChildren<SpriteRenderer>();

        NpcMovement dadMove = dad.GetComponent<NpcMovement>();
        if (dadMove == null)
        {
            dadMove = Undo.AddComponent<NpcMovement>(dad);
            EditorUtility.CopySerialized(stella, dadMove);   // same speed / ground settings
            Sprite[] frames = StellaSetup.LoadFrames(NoraWalkFolder, @"walk(?: nora)?(\d+)$", false, out int found);
            StellaSetup.AssignWalkFrames(dadMove, frames);
            if (renderer != null)
            {
                renderer.sprite = frames[0];
                renderer.color = new Color(0.75f, 0.85f, 1f);   // placeholder tint, so he doesn't look like Nora
            }
            report.Add("Dad uses Nora's walk frames as a placeholder (" + found + "/20 found, tinted blue).");
        }
        var moveSO = new SerializedObject(dadMove);
        moveSO.FindProperty("spriteRenderer").objectReferenceValue = renderer;
        moveSO.FindProperty("lookAtPlayerWhenIdle").boolValue = false;
        moveSO.ApplyModifiedProperties();

        // ---------- The scene ----------
        RoomEntranceScene scene = Object.FindFirstObjectByType<RoomEntranceScene>(FindObjectsInactive.Include);
        if (scene == null)
        {
            var go = new GameObject("Kitchen Entrance Scene");
            Undo.RegisterCreatedObjectUndo(go, "Create scene");
            scene = go.AddComponent<RoomEntranceScene>();
        }

        var so = new SerializedObject(scene);
        so.FindProperty("entranceDoor").objectReferenceValue = kitchenDoor;
        so.FindProperty("npc").objectReferenceValue = dadMove;
        so.FindProperty("busyLookAt").objectReferenceValue = lookAt;
        SerializedProperty points = so.FindProperty("busyPoints");
        points.arraySize = 2;
        points.GetArrayElementAtIndex(0).objectReferenceValue = pointA;
        points.GetArrayElementAtIndex(1).objectReferenceValue = pointB;

        var toDisable = new List<MonoBehaviour> { player };
        PlayerInteractor interactor = Object.FindFirstObjectByType<PlayerInteractor>(FindObjectsInactive.Include);
        if (interactor != null) toDisable.Add(interactor);
        SerializedProperty disable = so.FindProperty("disableDuringScene");
        disable.arraySize = toDisable.Count;
        for (int i = 0; i < toDisable.Count; i++)
            disable.GetArrayElementAtIndex(i).objectReferenceValue = toDisable[i];
        so.ApplyModifiedProperties();

        if (so.FindProperty("dialogue").objectReferenceValue == null)
            report.Add("TO DO: drag your first Dad line ('Oh Nora! Good to see you!') into 'Dialogue' (selected object).");

        Selection.activeGameObject = scene.gameObject;
        EditorSceneManager.MarkSceneDirty(dad.scene);
        EditorSceneManager.SaveScene(dad.scene);
        report.Add("Scene saved.");

        EditorUtility.DisplayDialog("Setup", "Done!\n\n- " + string.Join("\n- ", report), "OK");
    }

    // Tools > Sleepover > Setup Camera Moves
    // Creates CameraPoint_StellaReturn and CameraPoint_Kitchen (move them where the camera should go).
    [MenuItem("Tools/Sleepover/Setup Camera Moves")]
    public static void SetupCameraMoves()
    {
        var report = new List<string>();
        Scene? scene = null;

        NpcLeaveAndReturn sequence = Object.FindFirstObjectByType<NpcLeaveAndReturn>(FindObjectsInactive.Include);
        if (sequence != null)
        {
            var so = new SerializedObject(sequence);
            Transform spawn = so.FindProperty("returnSpawn").objectReferenceValue as Transform;
            Transform wait = so.FindProperty("waitPoint").objectReferenceValue as Transform;
            Vector3 pos = spawn != null && wait != null ? Vector3.Lerp(spawn.position, wait.position, 0.5f)
                        : sequence.transform.position;
            so.FindProperty("cameraPoint").objectReferenceValue = GetOrCreatePoint("CameraPoint_StellaReturn", pos, report);
            so.ApplyModifiedProperties();
            report.Add("Stella coming back: camera glides to 'CameraPoint_StellaReturn'.");
            scene = sequence.gameObject.scene;
        }

        RoomEntranceScene kitchen = Object.FindFirstObjectByType<RoomEntranceScene>(FindObjectsInactive.Include);
        if (kitchen != null)
        {
            var so = new SerializedObject(kitchen);
            RoomDoor door = so.FindProperty("entranceDoor").objectReferenceValue as RoomDoor;
            Transform table = so.FindProperty("busyLookAt").objectReferenceValue as Transform;
            Vector3 pos = door != null && door.Destination != null && table != null
                ? Vector3.Lerp(door.Destination.position, table.position, 0.6f)
                : kitchen.transform.position;
            so.FindProperty("cameraPoint").objectReferenceValue = GetOrCreatePoint("CameraPoint_Kitchen", pos, report);
            so.ApplyModifiedProperties();
            report.Add("Kitchen: camera glides to 'CameraPoint_Kitchen' toward Dad.");
            scene = kitchen.gameObject.scene;
        }

        if (scene == null)
        {
            EditorUtility.DisplayDialog("Camera moves", "Run 'Setup Stella Leaves & Returns' / 'Setup Kitchen Scene (Dad)' first.", "OK");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene.Value);
        EditorSceneManager.SaveScene(scene.Value);
        report.Add("Scene saved.");
        EditorUtility.DisplayDialog("Camera moves", "Done!\n\n- " + string.Join("\n- ", report), "OK");
    }

    // ---------------- Helpers ----------------

    // The biggest "Table" object inside the Kitchen (else a spot in front of the door).
    private static Bounds FindKitchenTable(Vector3 nearPoint, List<string> report)
    {
        Bounds best = new Bounds(nearPoint + Vector3.forward * 2f, Vector3.one);
        float bestSize = 0f;

        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!t.name.ToLower().Contains("table") || !HasAncestor(t, "Kitchen"))
                continue;

            Renderer[] renderers = t.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) continue;
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers) b.Encapsulate(r.bounds);

            float size = b.size.x * b.size.z;
            if (size > bestSize) { bestSize = size; best = b; }
        }

        report.Add(bestSize > 0f ? "Found the kitchen table." : "No kitchen table found: Dad's points are in front of the door (move them).");
        return best;
    }

    private static bool HasAncestor(Transform t, string name)
    {
        for (Transform p = t.parent; p != null; p = p.parent)
            if (p.name == name) return true;
        return false;
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
