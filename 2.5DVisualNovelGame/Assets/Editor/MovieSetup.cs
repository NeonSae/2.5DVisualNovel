using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Sleepover > Setup Movie Night
// After Nora sits next to Stella: TV on -> long fade to black -> music fades -> dialogue
// -> night lights -> Nora looks around, hops off the bed -> every door opens (Phase 4: Night)
// -> night music once she leaves Stella's room.
// Safe to run again: points you moved and slots you filled are kept.
public static class MovieSetup
{
    private const string MovieLineGuid = "3e864bf89988cbd4a8a4a109f4a14ced";    // 4. MOVIE/Before Sleep
    private const string WakeUpLineGuid = "446ec1d94af4c094ea5a3aafa186a8d7";   // 4. MOVIE/After Sleep
    private const string DayMusicPath = "Assets/Audio/Music/House/Day/Stella's Loop.mp3";
    private const string TvOnMaterialPath = "Assets/Art/VIRTI/VOXEL-Forniture/Models/LivingRoom/Materials/tvBig_screen 1.mat";

    [MenuItem("Tools/Sleepover/Setup Movie Night")]
    public static void Setup()
    {
        var report = new List<string>();

        DinnerSequence dinner = Object.FindFirstObjectByType<DinnerSequence>(FindObjectsInactive.Include);
        IntroSequence intro = Object.FindFirstObjectByType<IntroSequence>(FindObjectsInactive.Include);
        CharacterMovement player = Object.FindFirstObjectByType<CharacterMovement>(FindObjectsInactive.Include);
        RoomDoor stellaExit = FindDoor("Stella's room TO Corridor2");
        if (dinner == null || player == null || stellaExit == null)
        {
            EditorUtility.DisplayDialog("Setup Movie Night", "Run 'Setup Dinner (Kitchen)' first.", "OK");
            return;
        }

        MovieSequence movie = Object.FindFirstObjectByType<MovieSequence>(FindObjectsInactive.Include);
        if (movie == null)
        {
            var go = new GameObject("Movie Sequence === EVENT ===");
            Undo.RegisterCreatedObjectUndo(go, "Create Movie Sequence");
            movie = go.AddComponent<MovieSequence>();
            report.Add("Created 'Movie Sequence === EVENT ==='.");
        }

        // Starts when Nora sits.
        var dinnerSO = new SerializedObject(dinner);
        var sat = (UnityEngine.Events.UnityEvent)typeof(DinnerSequence)
            .GetField("onPlayerSat", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(dinner);
        bool wired = false;
        for (int i = 0; i < sat.GetPersistentEventCount(); i++)
            if (sat.GetPersistentTarget(i) == movie && sat.GetPersistentMethodName(i) == "Begin") wired = true;
        if (!wired) UnityEventTools.AddPersistentListener(sat, movie.Begin);
        EditorUtility.SetDirty(dinner);

        var so = new SerializedObject(movie);

        // ---------- TV ----------
        Transform tv = dinnerSO.FindProperty("tv").objectReferenceValue as Transform;
        Renderer screenRenderer = null;
        int slot = -1;
        if (tv != null)
        {
            foreach (Renderer r in tv.GetComponentsInChildren<Renderer>())
            {
                // The screen slot may have been swapped in the scene (e.g. to look "off"):
                // look at the original model's materials too.
                Renderer source = PrefabUtility.GetCorrespondingObjectFromSource(r);
                Material[] mats = r.sharedMaterials;
                Material[] original = source != null ? source.sharedMaterials : mats;
                for (int i = 0; i < mats.Length && slot < 0; i++)
                {
                    bool isScreen = (mats[i] != null && mats[i].name.ToLower().Contains("screen")) ||
                                    (i < original.Length && original[i] != null && original[i].name.ToLower().Contains("screen"));
                    if (isScreen) { screenRenderer = r; slot = i; }
                }
                if (slot >= 0) break;
            }
        }
        if (screenRenderer != null)
        {
            so.FindProperty("tvRenderer").objectReferenceValue = screenRenderer;
            so.FindProperty("screenMaterialSlot").intValue = slot;
            if (so.FindProperty("tvOnMaterial").objectReferenceValue == null && so.FindProperty("tvOnTexture").objectReferenceValue == null)
                so.FindProperty("tvOnMaterial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(TvOnMaterialPath);
            report.Add("TV screen: '" + screenRenderer.name + "' slot " + slot + " (swap 'Tv On Material' / 'Tv On Texture' for your picture).");
        }
        else
        {
            report.Add("! TV screen not found: drag the TV renderer into 'Tv Renderer'.");
        }

        // ---------- Black screen / dialogue ----------
        if (intro != null)
        {
            var introSO = new SerializedObject(intro);
            so.FindProperty("screenFader").objectReferenceValue = introSO.FindProperty("screenFader").objectReferenceValue;
            so.FindProperty("dialogueCanvas").objectReferenceValue = introSO.FindProperty("dialogueCanvas").objectReferenceValue;
            so.FindProperty("dialogueManager").objectReferenceValue = introSO.FindProperty("dialogueManager").objectReferenceValue;
        }
        if (so.FindProperty("movieDialogue").objectReferenceValue == null)
        {
            var line = AssetDatabase.LoadAssetAtPath<DialogueNode>(AssetDatabase.GUIDToAssetPath(MovieLineGuid));
            so.FindProperty("movieDialogue").objectReferenceValue = line;
            if (line != null) report.Add("Movie dialogue = 4. MOVIE/" + line.name + " (change it if needed).");
        }

        // ---------- Night ----------
        GameObject nightLights = FindInScene("NightLights");
        GameObject dayLights = FindInScene("DayLights");
        SetArray(so.FindProperty("nightObjects"), nightLights);
        SetArray(so.FindProperty("dayObjects"), dayLights);

        // Stella is gone when Nora wakes up.
        NpcMovement stella = dinnerSO.FindProperty("stella").objectReferenceValue as NpcMovement;
        if (stella != null)
        {
            SetArray(so.FindProperty("goneAtNight"), stella.gameObject);
            report.Add("Stella disappears while the screen is black.");
        }
        if (so.FindProperty("wakeUpDialogue").objectReferenceValue == null)
        {
            var wake = AssetDatabase.LoadAssetAtPath<DialogueNode>(AssetDatabase.GUIDToAssetPath(WakeUpLineGuid));
            so.FindProperty("wakeUpDialogue").objectReferenceValue = wake;
            if (wake != null) report.Add("After looking around: 4. MOVIE/" + wake.name + ", then she hops off.");
        }
        report.Add((nightLights != null ? "NightLights ON" : "! NightLights not found") + ", " +
                   (dayLights != null ? "DayLights OFF" : "! DayLights not found") + " after the dialogue.");

        // ---------- Nora wakes up ----------
        PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
        if (interactor == null) interactor = Object.FindFirstObjectByType<PlayerInteractor>(FindObjectsInactive.Include);
        SetArray(so.FindProperty("playerScripts"), player, interactor);

        Transform noraSeat = dinnerSO.FindProperty("playerMovieSeat").objectReferenceValue as Transform;
        Transform couchSeat = dinnerSO.FindProperty("couchSeat").objectReferenceValue as Transform;
        Transform couchFront = dinnerSO.FindProperty("couchFront").objectReferenceValue as Transform;
        if (noraSeat != null)
        {
            Vector3 off = couchFront != null && couchSeat != null ? couchFront.position - couchSeat.position : Vector3.back;
            off.y = 0f;
            Vector3 stand = noraSeat.position + off;
            stand.y = couchFront != null ? couchFront.position.y : noraSeat.position.y;
            so.FindProperty("standPoint").objectReferenceValue = GetOrCreatePoint("NoraPoint_BedStand", stand, report);
        }

        // ---------- Everything open ----------
        InteractionLock exitLock = stellaExit.GetComponent<InteractionLock>();
        SetArray(so.FindProperty("unlock"), exitLock);
        so.FindProperty("musicDoor").objectReferenceValue = stellaExit;
        if (so.FindProperty("nightMusic").objectReferenceValue == null)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Music/House/Night" }))
            {
                so.FindProperty("nightMusic").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guid));
                break;
            }
        }
        var music = so.FindProperty("nightMusic").objectReferenceValue;
        report.Add("Night music when leaving Stella's room: " + (music != null ? music.name : "! none (drag one into 'Night Music')") + ".");
        so.ApplyModifiedProperties();

        // Phase 4: every door that was closed "forever" opens at night.
        int opened = 0;
        foreach (PhaseDoor door in Object.FindObjectsByType<PhaseDoor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var doorSO = new SerializedObject(door);
            opened += OpenAtNight(doorSO.FindProperty("closedFrom"), doorSO.FindProperty("openedFrom"));
            SerializedProperty times = doorSO.FindProperty("moreClosedTimes");
            for (int i = 0; i < times.arraySize; i++)
                opened += OpenAtNight(times.GetArrayElementAtIndex(i).FindPropertyRelative("closedFrom"),
                                      times.GetArrayElementAtIndex(i).FindPropertyRelative("openedFrom"));
            doorSO.ApplyModifiedProperties();
        }
        report.Add("Phase 4: Night: all closed doors open (" + opened + " closed time(s) now end at Night).");

        Selection.activeGameObject = movie.gameObject;
        EditorSceneManager.MarkSceneDirty(movie.gameObject.scene);
        EditorSceneManager.SaveScene(movie.gameObject.scene);
        report.Add("Scene saved.");
        EditorUtility.DisplayDialog("Setup Movie Night", "Done!\n\n- " + string.Join("\n- ", report), "OK");
    }

    // Tools > Sleepover > Setup Phase Checkpoints (Testing)
    // Story Phases > "Starting Phase" then starts the game at that point (Phase 2/3/4).
    [MenuItem("Tools/Sleepover/Setup Phase Checkpoints (Testing)")]
    public static void SetupCheckpoints()
    {
        StoryPhases phases = Object.FindFirstObjectByType<StoryPhases>(FindObjectsInactive.Include);
        if (phases == null)
        {
            EditorUtility.DisplayDialog("Checkpoints", "No 'Story Phases' in the scene (use Tools > Closed Doors once).", "OK");
            return;
        }

        PhaseCheckpoints checkpoints = phases.GetComponent<PhaseCheckpoints>();
        if (checkpoints == null) checkpoints = Undo.AddComponent<PhaseCheckpoints>(phases.gameObject);

        NpcMovement stella = null;
        foreach (NpcMovement npc in Object.FindObjectsByType<NpcMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (npc.name == "Stella") stella = npc;

        var so = new SerializedObject(checkpoints);
        so.FindProperty("storyPhases").objectReferenceValue = phases;
        so.FindProperty("intro").objectReferenceValue = Object.FindFirstObjectByType<IntroSequence>(FindObjectsInactive.Include);
        so.FindProperty("stella").objectReferenceValue = stella;
        so.FindProperty("stellaGuide").objectReferenceValue = stella != null ? stella.GetComponent<NpcGuide>() : null;
        so.FindProperty("stellaLeavesAndReturns").objectReferenceValue = Object.FindFirstObjectByType<NpcLeaveAndReturn>(FindObjectsInactive.Include);
        so.FindProperty("kitchenEntrance").objectReferenceValue = Object.FindFirstObjectByType<RoomEntranceScene>(FindObjectsInactive.Include);
        so.FindProperty("dinner").objectReferenceValue = Object.FindFirstObjectByType<DinnerSequence>(FindObjectsInactive.Include);
        so.FindProperty("movie").objectReferenceValue = Object.FindFirstObjectByType<MovieSequence>(FindObjectsInactive.Include);
        GameObject kitchenSpot = FindInScene("StellaPoint_Kitchen");
        so.FindProperty("stellaKitchenSpot").objectReferenceValue = kitchenSpot != null ? kitchenSpot.transform : null;
        so.FindProperty("kitchenDoor").objectReferenceValue = FindDoor("Corridor1 TO Kitchen");
        so.FindProperty("stellasRoomDoor").objectReferenceValue = FindDoor("Corridor2 TO Stella's room");
        var dayMusic = AssetDatabase.LoadAssetAtPath<AudioClip>(DayMusicPath);
        if (so.FindProperty("kitchenMusic").objectReferenceValue == null) so.FindProperty("kitchenMusic").objectReferenceValue = dayMusic;
        if (so.FindProperty("movieMusic").objectReferenceValue == null) so.FindProperty("movieMusic").objectReferenceValue = dayMusic;
        so.ApplyModifiedProperties();

        Selection.activeGameObject = phases.gameObject;
        EditorSceneManager.MarkSceneDirty(phases.gameObject.scene);
        EditorSceneManager.SaveScene(phases.gameObject.scene);
        EditorUtility.DisplayDialog("Checkpoints",
            "Done!\n\nOn 'Story Phases' (selected), set 'Starting Phase' to:\n" +
            "- Phase 1: skip the intro\n- Phase 2: Kitchen: walk into the kitchen\n" +
            "- Phase 3: Movie: in Stella's room, ready to Sit\n- Phase 4: Night: after the movie\n\n" +
            "Put it back on 'Start of the game' for the real game.\nScene saved.", "OK");
    }

    // A closed time that reaches past Night now ends at Night.
    private static int OpenAtNight(SerializedProperty from, SerializedProperty until)
    {
        if (from.intValue >= (int)StoryPhase.Night || until.intValue <= (int)StoryPhase.Night)
            return 0;
        until.enumValueIndex = System.Array.IndexOf(System.Enum.GetValues(typeof(StoryPhase)), StoryPhase.Night);
        return 1;
    }

    private static void SetArray(SerializedProperty array, params Object[] values)
    {
        var list = new List<Object>();
        foreach (Object v in values) if (v != null) list.Add(v);
        array.arraySize = list.Count;
        for (int i = 0; i < list.Count; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
    }

    private static GameObject FindInScene(string name)
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == name && t.gameObject.scene.IsValid())
                return t.gameObject;
        return null;
    }

    private static Transform GetOrCreatePoint(string name, Vector3 position, List<string> report)
    {
        GameObject existing = FindInScene(name);
        if (existing != null)
            return existing.transform;

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create point");
        go.transform.position = position;
        report.Add("Created '" + name + "' (where Nora lands off the bed, check it).");
        return go.transform;
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
