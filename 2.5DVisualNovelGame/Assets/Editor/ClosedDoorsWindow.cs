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
    private bool addAsExtra;

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
        addAsExtra = EditorGUILayout.ToggleLeft(
            new GUIContent("Add as an EXTRA closed time (keeps what the door already has)",
                "On: the door gets one more closed time with this line. Off: replaces the door's main closed time."), addAsExtra);

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

            SetClosed(door, closedFrom, openedFrom, line, addAsExtra);
            done++;
        }

        EnsureStoryPhases();

        if (doors.Count > 0)
        {
            EditorSceneManager.MarkSceneDirty(doors[0].scene);
            EditorSceneManager.SaveScene(doors[0].scene);
        }

        string text = done + " door(s) set: closed from '" + closedFrom + "', open from '" + openedFrom + "'" +
                      (addAsExtra ? " (extra closed time)" : "") + ".\nScene saved.";
        if (skipped.Count > 0) text += "\n\nSkipped:\n- " + string.Join("\n- ", skipped);
        EditorUtility.DisplayDialog("Closed Doors", text, "OK");
        newLine = "";
    }

    // Phase 3 (Movie) starts when leaving the kitchen after dinner: only the way to
    // Stella's room is open, every other corridor door says "Let's go to Stella's room...".
    [MenuItem("Tools/Sleepover/Setup Movie Phase Doors")]
    public static void SetupMoviePhase()
    {
        var report = new List<string>();
        EnsureStoryPhases();

        // Leaving the kitchen -> Phase 3.
        StoryPhases phases = Object.FindFirstObjectByType<StoryPhases>(FindObjectsInactive.Include);
        RoomDoor kitchenExit = FindDoor("Kitchen TO Corridor1");
        if (phases != null && kitchenExit != null)
        {
            var so = new SerializedObject(phases);
            SerializedProperty starts = so.FindProperty("phaseStarts");
            bool listed = false;
            for (int i = 0; i < starts.arraySize; i++)
                if (starts.GetArrayElementAtIndex(i).FindPropertyRelative("door").objectReferenceValue == kitchenExit) listed = true;
            if (!listed)
            {
                starts.arraySize++;
                SerializedProperty start = starts.GetArrayElementAtIndex(starts.arraySize - 1);
                start.FindPropertyRelative("door").objectReferenceValue = kitchenExit;
                SetPhase(start.FindPropertyRelative("phase"), StoryPhase.Movie);
                so.ApplyModifiedProperties();
            }
            report.Add("Phase 3: Movie starts when you leave the kitchen.");
        }

        // The line for the closed doors (made once).
        const string text = "Let's go to Stella's room to watch a movie.";
        DialogueNode line = null;
        foreach (string guid in AssetDatabase.FindAssets("t:DialogueNode", new[] { DoorsFolder }))
        {
            var node = AssetDatabase.LoadAssetAtPath<DialogueNode>(AssetDatabase.GUIDToAssetPath(guid));
            if (node != null && node.text == text) line = node;
        }
        if (line == null) line = CreateLine(text);

        string[] closed =
        {
            "Corridor1 TO Bathroom", "Corridor1 TO Kitchen", "Corridor1 TO Living room",
            "Corridor2 TO Corridor1", "Corridor2 TO Guests' room", "Corridor2 TO Parents' room"
        };
        foreach (string name in closed)
        {
            RoomDoor door = FindDoor(name);
            if (door == null) { report.Add("! Not found: " + name); continue; }
            SetClosed(door.gameObject, StoryPhase.Movie, StoryPhase.Never, line, true);
        }
        report.Add("Closed in Phase 3 (\"" + text + "\"): " + string.Join(", ", closed) + ".");

        // The way to her room is open in Phase 3.
        RoomDoor toStella = FindDoor("Corridor2 TO Stella's room");
        PhaseDoor toStellaPhase = toStella != null ? toStella.GetComponent<PhaseDoor>() : null;
        if (toStellaPhase != null)
        {
            var so = new SerializedObject(toStellaPhase);
            if (so.FindProperty("openedFrom").intValue > (int)StoryPhase.Movie)
                SetPhase(so.FindProperty("openedFrom"), StoryPhase.Movie);
            so.ApplyModifiedProperties();
        }
        report.Add("Open in Phase 3: Corridor1 TO Corridor2, Corridor2 TO Stella's room.");

        if (phases != null)
        {
            EditorSceneManager.MarkSceneDirty(phases.gameObject.scene);
            EditorSceneManager.SaveScene(phases.gameObject.scene);
            report.Add("Scene saved.");
        }
        EditorUtility.DisplayDialog("Movie phase", "Done!\n\n- " + string.Join("\n- ", report), "OK");
    }

    // main = the door's main closed time (message on the Blocked Door);
    // extra = one more closed time with its own line (replaces an extra time with the same phases).
    public static void SetClosed(GameObject door, StoryPhase from, StoryPhase until, DialogueNode line, bool extra)
    {
        BlockedDoor blocker = door.GetComponent<BlockedDoor>();
        bool newBlocker = blocker == null;
        if (newBlocker) blocker = Undo.AddComponent<BlockedDoor>(door);

        PhaseDoor phaseDoor = door.GetComponent<PhaseDoor>();
        bool newPhaseDoor = phaseDoor == null;
        if (newPhaseDoor) phaseDoor = Undo.AddComponent<PhaseDoor>(door);
        var so = new SerializedObject(phaseDoor);

        if (extra)
        {
            // A door that had no phases yet: its main time does nothing.
            if (newPhaseDoor)
            {
                SetPhase(so.FindProperty("closedFrom"), StoryPhase.Never);
                SetPhase(so.FindProperty("openedFrom"), StoryPhase.Never);
            }

            SerializedProperty times = so.FindProperty("moreClosedTimes");
            SerializedProperty time = null;
            for (int i = 0; i < times.arraySize && time == null; i++)
            {
                SerializedProperty t = times.GetArrayElementAtIndex(i);
                if (t.FindPropertyRelative("closedFrom").intValue == (int)from && t.FindPropertyRelative("openedFrom").intValue == (int)until)
                    time = t;
            }
            if (time == null)
            {
                times.arraySize++;
                time = times.GetArrayElementAtIndex(times.arraySize - 1);
            }
            SetPhase(time.FindPropertyRelative("closedFrom"), from);
            SetPhase(time.FindPropertyRelative("openedFrom"), until);
            if (line != null) time.FindPropertyRelative("message").objectReferenceValue = line;
        }
        else
        {
            SetPhase(so.FindProperty("closedFrom"), from);
            SetPhase(so.FindProperty("openedFrom"), until);
            if (line != null)
            {
                var blockSO = new SerializedObject(blocker);
                blockSO.FindProperty("blockedDialogue").objectReferenceValue = line;
                blockSO.ApplyModifiedProperties();
            }
        }
        so.ApplyModifiedProperties();
    }

    private static void SetPhase(SerializedProperty prop, StoryPhase phase)
    {
        prop.enumValueIndex = System.Array.IndexOf(System.Enum.GetValues(typeof(StoryPhase)), phase);
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

    public static DialogueNode CreateLine(string text)
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

    public static RoomDoor FindDoor(string doorObjectName)
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
