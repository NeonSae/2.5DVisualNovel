using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Sleepover > Setup Intro
// Wires the opening (narration, doorbell, cutscene, door closing, first RPG dialogue)
// in the open game scene. Assets are found by ID, so moving folders doesn't break it.
public static class IntroSetup
{
    // Dialogue
    private const string Intro1 = "47aded6dee6a4d2588cfcd6838193b21";
    private const string Intro2 = "81e2204353854f0e92f5e4fbe62aa4fd";
    private const string Intro3 = "12ddec1eff2f45f1846f443318395b90";
    private const string Cutscene1_00 = "843d99ea7f5c00a49969850f4617aa67";
    private const string Cutscene1_03 = "76c1e3e1bca34c3f9c4c816d36f26815";
    private const string Rpg1 = "60114eae03c673543b78a86ebeb16e3c";
    private const string Rpg2 = "23c617099bc6c734fa273f652d165d06";
    private const string RpgChoice = "a6a9383d065cc5e45a9e98287443a92e";

    // Audio
    private const string Doorbell = "823f10cc7c56022438a799bb1fa21b4e";
    private const string DoorOpening = "40cd69781a7c80f46b684d069ab090e2";
    private const string DoorClosed = "04ea5494919ee4a4c9eb87bd5e74c87f";
    private const string DoorLoop = "347bc004293b4b94c8c2133fc56d9f43";

    private const string ItsThisWay = "18f4623495422c54ea71e58b5a2fe98a"; // RPGMODE1/InteractWDoor

    // "Explore a little longer" ends the talk; 15s later Stella says "Nora? it's this way".
    [MenuItem("Tools/Sleepover/Setup Explore Reminder (Living Room)")]
    public static void SetupExploreReminder()
    {
        IntroSequence intro = Object.FindFirstObjectByType<IntroSequence>(FindObjectsInactive.Include);
        DialogueNode choice = Load<DialogueNode>(RpgChoice);
        if (intro == null || choice == null || choice.choices == null || choice.choices.Length == 0)
        {
            EditorUtility.DisplayDialog("Setup", "Need the Intro Sequence in the scene and RPGMODE1/Choice with its choices.", "OK");
            return;
        }

        // The "explore" choice now just closes the dialogue.
        Undo.RecordObject(choice, "Explore choice");
        choice.choices[0].nextNode = null;
        EditorUtility.SetDirty(choice);
        AssetDatabase.SaveAssets();

        ChoiceReminder reminder = intro.GetComponent<ChoiceReminder>();
        if (reminder == null) reminder = Undo.AddComponent<ChoiceReminder>(intro.gameObject);
        var so = new SerializedObject(reminder);
        so.FindProperty("dialogueWithChoice").objectReferenceValue = Load<DialogueNode>(Rpg1);
        so.FindProperty("button").intValue = 0;
        so.FindProperty("delay").floatValue = 15f;
        so.FindProperty("reminderDialogue").objectReferenceValue = Load<DialogueNode>(ItsThisWay);
        so.ApplyModifiedProperties();

        Selection.activeGameObject = intro.gameObject;
        EditorSceneManager.MarkSceneDirty(intro.gameObject.scene);
        EditorSceneManager.SaveScene(intro.gameObject.scene);
        EditorUtility.DisplayDialog("Setup",
            "Done!\n\n- 'Explore a little longer' now just closes the dialogue.\n" +
            "- 15s later (paused while reading), Stella says 'Nora? it's this way'.\n" +
            "- Settings: 'Choice Reminder' on the Intro Sequence object.\n- Scene saved.", "OK");
    }

    [MenuItem("Tools/Sleepover/Setup Intro")]
    public static void Setup()
    {
        var report = new List<string>();

        // ---------- Dialogue assets ----------
        SetSpeaker(Intro1, "Nora", report);
        SetSpeaker(Intro2, "Nora", report);
        SetSpeaker(Intro3, "Nora", report);

        SetObject(Cutscene1_03, "startMusic", Load<AudioClip>(DoorLoop), report, "Door Loop starts on Cutscene1_03");

        SetSpeaker(Rpg1, "Nora", report);
        SetObject(Rpg1, "nextNode", Load<DialogueNode>(Rpg2), report, "RPGMODE1: 1 -> 2");
        SetObject(Rpg2, "nextNode", Load<DialogueNode>(RpgChoice), report, "RPGMODE1: 2 -> Choice");

        AssetDatabase.SaveAssets();

        // ---------- Scene ----------
        IntroSequence intro = Object.FindFirstObjectByType<IntroSequence>(FindObjectsInactive.Include);
        if (intro == null)
        {
            EditorUtility.DisplayDialog("Setup Intro",
                "No 'Intro Sequence' found in the open scene. Open SampleScene and try again.", "OK");
            return;
        }

        intro.gameObject.SetActive(true);
        intro.enabled = true;

        var so = new SerializedObject(intro);
        so.FindProperty("narrationDialogue").objectReferenceValue = Load<DialogueNode>(Intro1);
        so.FindProperty("cutsceneDialogue").objectReferenceValue = Load<DialogueNode>(Cutscene1_00);
        so.FindProperty("followUpDialogue").objectReferenceValue = Load<DialogueNode>(Rpg1);
        so.FindProperty("introMusic").objectReferenceValue = null;
        so.FindProperty("doorClosingSound").objectReferenceValue = Load<AudioClip>(DoorClosed);

        SetArray(so.FindProperty("soundsBeforeCutscene"),
            Load<AudioClip>(Doorbell), Load<AudioClip>(DoorOpening));

        // Freeze the player during the intro.
        var player = Object.FindFirstObjectByType<CharacterMovement>(FindObjectsInactive.Include);
        if (player != null)
        {
            var interactor = player.GetComponent<PlayerInteractor>();
            SetArray(so.FindProperty("disableDuringIntro"), player, interactor);
        }
        else
        {
            report.Add("! No CharacterMovement found (player not frozen during intro).");
        }

        so.ApplyModifiedProperties();
        report.Add("Intro Sequence wired.");

        // Music comes from the cutscene, not from the AudioManager at start.
        var audio = Object.FindFirstObjectByType<AudioManager>(FindObjectsInactive.Include);
        if (audio != null)
        {
            var audioSO = new SerializedObject(audio);
            audioSO.FindProperty("playMusicOnStart").boolValue = false;
            audioSO.ApplyModifiedProperties();
            report.Add("AudioManager: Play Music On Start turned off.");
        }

        EditorSceneManager.MarkSceneDirty(intro.gameObject.scene);
        EditorSceneManager.SaveScene(intro.gameObject.scene);
        report.Add("Scene saved.");

        EditorUtility.DisplayDialog("Setup Intro", "Done!\n\n- " + string.Join("\n- ", report), "OK");
    }

    // ---------------- Helpers ----------------

    private static T Load<T>(string guid) where T : Object
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
    }

    private static void SetSpeaker(string nodeGuid, string speaker, List<string> report)
    {
        DialogueNode node = Load<DialogueNode>(nodeGuid);
        if (node == null)
        {
            report.Add("! Missing dialogue " + nodeGuid);
            return;
        }

        var so = new SerializedObject(node);
        so.FindProperty("speaker").stringValue = speaker;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(node);
    }

    private static void SetObject(string nodeGuid, string field, Object value, List<string> report, string label)
    {
        DialogueNode node = Load<DialogueNode>(nodeGuid);
        if (node == null || value == null)
        {
            report.Add("! Could not set: " + label);
            return;
        }

        var so = new SerializedObject(node);
        so.FindProperty(field).objectReferenceValue = value;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(node);
        report.Add(label);
    }

    private static void SetArray(SerializedProperty array, params Object[] values)
    {
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
