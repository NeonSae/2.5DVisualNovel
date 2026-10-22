using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Sleepover > Closed Doors...
// Select doors in the Hierarchy, pick "Closed from" / "Opened from" and the line Nora says,
// then click Apply. Phases: Start of the game -> Phase 1 (leaving the living room)
// -> Phase 2 (leaving Stella's room).
public class ClosedDoorsWindow : EditorWindow
{
    private const string DoorsFolder = "Assets/Data/Dialogue/_Optionals/Doors";

    private StoryPhase closedFrom = StoryPhase.StartOfGame;
    private StoryPhase openedFrom = StoryPhase.Kitchen;
    private DialogueNode message;
    private string newLine = "";

    [MenuItem("Tools/Sleepover/Closed Doors...")]
    public static void Open()
    {
        GetWindow<ClosedDoorsWindow>("Closed Doors").minSize = new Vector2(340, 300);
    }

    private void OnSelectionChange() => Repaint();

    private void OnGUI()
    {
        List<GameObject> doors = SelectedDoors();

        EditorGUILayout.HelpBox(
            "1. Select door objects in the Hierarchy (Ctrl+click for several).\n" +
            "2. Choose when they are closed / open, and what Nora says.\n" +
            "3. Click Apply.", MessageType.Info);

        EditorGUILayout.LabelField("Selected doors: " + doors.Count, EditorStyles.boldLabel);
        foreach (GameObject door in doors)
            EditorGUILayout.LabelField("   • " + DoorLabel(door));

        EditorGUILayout.Space();
        closedFrom = (StoryPhase)EditorGUILayout.EnumPopup("Closed from", closedFrom);
        openedFrom = (StoryPhase)EditorGUILayout.EnumPopup("Opened from", openedFrom);
        if (openedFrom <= closedFrom)
            EditorGUILayout.HelpBox("'Opened from' must be after 'Closed from'.", MessageType.Warning);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("What Nora says when trying it", EditorStyles.boldLabel);
        message = (DialogueNode)EditorGUILayout.ObjectField("Existing dialogue", message, typeof(DialogueNode), false);
        using (new EditorGUI.DisabledScope(message != null))
            newLine = EditorGUILayout.TextField("...or type a new line", newLine);
        EditorGUILayout.LabelField("(Empty = keep the door's current message.)", EditorStyles.miniLabel);

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(doors.Count == 0 || openedFrom <= closedFrom))
        {
            if (GUILayout.Button("Apply to selected doors", GUILayout.Height(30)))
                Apply(doors);
        }
    }

    private void Apply(List<GameObject> doors)
    {
        DialogueNode line = message != null ? message : CreateLine(newLine);
        var skipped = new List<string>();
        int done = 0;

        foreach (GameObject door in doors)
        {
            // Stella's room door is opened by "Stella Leaves & Returns" (after she goes through it).
            NpcLeaveAndReturn sequence = Object.FindFirstObjectByType<NpcLeaveAndReturn>(FindObjectsInactive.Include);
            BlockedDoor existing = door.GetComponent<BlockedDoor>();
            if (sequence != null && existing != null && ListsDoor(sequence, existing))
            {
                skipped.Add(DoorLabel(door) + " (Stella opens it herself)");
                continue;
            }

            BlockedDoor blocker = existing != null ? existing : Undo.AddComponent<BlockedDoor>(door);
            if (line != null)
            {
                var blockSO = new SerializedObject(blocker);
                blockSO.FindProperty("blockedDialogue").objectReferenceValue = line;
                blockSO.ApplyModifiedProperties();
            }

            PhaseDoor phaseDoor = door.GetComponent<PhaseDoor>();
            if (phaseDoor == null) phaseDoor = Undo.AddComponent<PhaseDoor>(door);
            var so = new SerializedObject(phaseDoor);
            so.FindProperty("closedFrom").enumValueIndex = System.Array.IndexOf(System.Enum.GetValues(typeof(StoryPhase)), closedFrom);
            so.FindProperty("openedFrom").enumValueIndex = System.Array.IndexOf(System.Enum.GetValues(typeof(StoryPhase)), openedFrom);
            so.ApplyModifiedProperties();
            done++;
        }

        EnsureStoryPhases();

        if (doors.Count > 0)
        {
            EditorSceneManager.MarkSceneDirty(doors[0].scene);
            EditorSceneManager.SaveScene(doors[0].scene);
        }

        string text = done + " door(s) set: closed from '" + closedFrom + "', open from '" + openedFrom + "'.\nScene saved.";
        if (skipped.Count > 0) text += "\n\nSkipped:\n- " + string.Join("\n- ", skipped);
        EditorUtility.DisplayDialog("Closed Doors", text, "OK");
        newLine = "";
    }

    // The scene object that changes phase when the player goes through the right doors.
    private static void EnsureStoryPhases()
    {
        StoryPhases phases = Object.FindFirstObjectByType<StoryPhases>(FindObjectsInactive.Include);
        if (phases == null)
        {
            var go = new GameObject("Story Phases");
            Undo.RegisterCreatedObjectUndo(go, "Create Story Phases");
            phases = go.AddComponent<StoryPhases>();
        }

        var so = new SerializedObject(phases);
        SerializedProperty starts = so.FindProperty("phaseStarts");
        if (starts.arraySize > 0)
            return;   // keep what the user set

        RoomDoor leaveLiving = FindDoor("Living room TO Corridor1");
        RoomDoor leaveStella = FindDoor("Stella's room TO Corridor2");
        starts.arraySize = 2;
        starts.GetArrayElementAtIndex(0).FindPropertyRelative("door").objectReferenceValue = leaveLiving;
        starts.GetArrayElementAtIndex(0).FindPropertyRelative("phase").enumValueIndex = 1; // Phase 1
        starts.GetArrayElementAtIndex(1).FindPropertyRelative("door").objectReferenceValue = leaveStella;
        starts.GetArrayElementAtIndex(1).FindPropertyRelative("phase").enumValueIndex = 2; // Phase 2
        so.ApplyModifiedProperties();
    }

    private static bool ListsDoor(NpcLeaveAndReturn sequence, BlockedDoor door)
    {
        SerializedProperty list = new SerializedObject(sequence).FindProperty("unblockDoors");
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == door) return true;
        return false;
    }

    private static DialogueNode CreateLine(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        if (!AssetDatabase.IsValidFolder(DoorsFolder))
            AssetDatabase.CreateFolder("Assets/Data/Dialogue/_Optionals", "Doors");

        string fileName = text.Trim();
        foreach (char c in System.IO.Path.GetInvalidFileNameChars()) fileName = fileName.Replace(c, ' ');
        if (fileName.Length > 40) fileName = fileName.Substring(0, 40);

        var node = CreateInstance<DialogueNode>();
        node.speaker = "Nora";
        node.text = text.Trim();
        AssetDatabase.CreateAsset(node, AssetDatabase.GenerateUniqueAssetPath(DoorsFolder + "/" + fileName.Trim() + ".asset"));
        AssetDatabase.SaveAssets();
        return node;
    }

    // The objects that actually hold a door script, from what's selected.
    private static List<GameObject> SelectedDoors()
    {
        var result = new List<GameObject>();
        foreach (GameObject go in Selection.gameObjects)
        {
            RoomDoor roomDoor = go.GetComponentInChildren<RoomDoor>(true);
            LockedRoomDoor lockedDoor = go.GetComponentInChildren<LockedRoomDoor>(true);
            GameObject target = roomDoor != null ? roomDoor.gameObject : lockedDoor != null ? lockedDoor.gameObject : null;
            if (target != null && !result.Contains(target)) result.Add(target);
        }
        return result;
    }

    private static string DoorLabel(GameObject door)
    {
        // Doors are usually named on a parent ("Corridor1 TO Kitchen").
        for (Transform t = door.transform; t != null; t = t.parent)
            if (t.name.Contains(" TO ")) return t.name;
        return door.name;
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
