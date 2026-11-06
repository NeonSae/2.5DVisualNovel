using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Sleepover > Fix Kitchen Table (After Dinner)  -> the table's Inspect works after eating
// Tools > Sleepover > Check Everything                   -> report of problems in Temp/GameCheck.txt
public static class GameCheck
{
    private const string ReportPath = "Temp/GameCheck.txt";

    [MenuItem("Tools/Sleepover/Fix Kitchen Table (After Dinner)")]
    public static void FixKitchenTable()
    {
        DinnerSequence dinner = Object.FindFirstObjectByType<DinnerSequence>(FindObjectsInactive.Include);
        SitDown table = Object.FindObjectsByType<SitDown>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(s => s.name == "TableComplete");
        if (dinner == null || table == null)
        {
            EditorUtility.DisplayDialog("Kitchen Table", "No DinnerSequence or TableComplete in the open scene.", "OK");
            return;
        }

        var report = new List<string>();
        var locks = new List<InteractionLock>();
        foreach (InspectDialogue inspect in table.GetComponentsInChildren<InspectDialogue>(true))
        {
            GameObject go = inspect.gameObject;
            Undo.RecordObject(go, "Fix table");
            go.SetActive(true);   // stays hidden by its lock until dinner is over

            InteractionLock l = go.GetComponent<InteractionLock>();
            if (l == null) l = Undo.AddComponent<InteractionLock>(go);
            var lockSO = new SerializedObject(l);
            lockSO.FindProperty("lockedAtStart").boolValue = true;
            lockSO.ApplyModifiedProperties();
            locks.Add(l);

            foreach (Collider c in go.GetComponents<Collider>())
            {
                Undo.RecordObject(c, "Fix table");
                c.isTrigger = true;   // a solid box around the table would trap Nora in her seat
            }
            report.Add("'" + go.name + "' (" + inspect.InteractionPrompt + "): shows up after dinner, collider is now a trigger.");
        }

        if (locks.Count == 0)
        {
            EditorUtility.DisplayDialog("Kitchen Table", "No InspectDialogue under TableComplete.\n" +
                "Add a child with an Inspect Dialogue first (Tools > Make Selected Objects Inspectable).", "OK");
            return;
        }

        var dinnerSO = new SerializedObject(dinner);
        SerializedProperty list = dinnerSO.FindProperty("unlockAfterDinner");
        list.arraySize = locks.Count;
        for (int i = 0; i < locks.Count; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = locks[i];
        dinnerSO.ApplyModifiedProperties();

        // The old "Sit down > SetActive(true)" isn't needed anymore (harmless, but removed to keep it clean).
        var sitSO = new SerializedObject(table);
        SerializedProperty calls = sitSO.FindProperty("onSit.m_PersistentCalls.m_Calls");
        for (int i = calls.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty call = calls.GetArrayElementAtIndex(i);
            Object target = call.FindPropertyRelative("m_Target").objectReferenceValue;
            if (call.FindPropertyRelative("m_MethodName").stringValue == "SetActive" &&
                target is GameObject g && locks.Any(l => l.gameObject == g))
                calls.DeleteArrayElementAtIndex(i);
        }
        sitSO.ApplyModifiedProperties();

        EditorSceneManager.MarkSceneDirty(dinner.gameObject.scene);
        EditorSceneManager.SaveScene(dinner.gameObject.scene);
        EditorUtility.DisplayDialog("Kitchen Table", "Done! Scene saved.\n\n- " + string.Join("\n- ", report), "OK");
    }

    [MenuItem("Tools/Sleepover/Check Everything")]
    public static void CheckEverything()
    {
        var problems = new StringBuilder();
        var notes = new StringBuilder();
        int count = 0;
        void Problem(string s) { problems.AppendLine("- " + s); count++; }

        GameObject[] all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Select(t => t.gameObject).ToArray();

        // Who references what (to find locks nothing ever unlocks).
        var referenced = new HashSet<Object>();

        foreach (GameObject go in all)
        {
            int missing = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            if (missing > 0) Problem(PathOf(go) + ": " + missing + " missing script(s).");

            foreach (Component comp in go.GetComponents<Component>())
            {
                if (comp == null) continue;
                var so = new SerializedObject(comp);
                SerializedProperty p = so.GetIterator();
                while (p.Next(true))
                {
                    if (p.propertyType != SerializedPropertyType.ObjectReference) continue;
                    if (p.objectReferenceValue != null)
                    {
                        if (p.objectReferenceValue != comp && p.objectReferenceValue != go) referenced.Add(p.objectReferenceValue);
                    }
                    else if (p.objectReferenceInstanceIDValue != 0)
                    {
                        Problem(PathOf(go) + " > " + comp.GetType().Name + " > " + p.displayName + ": points to something deleted (Missing).");
                    }
                }
            }
        }

        foreach (InteractionLock l in Object.FindObjectsByType<InteractionLock>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!new SerializedObject(l).FindProperty("lockedAtStart").boolValue) continue;
            if (!referenced.Contains(l) && !referenced.Contains(l.gameObject))
                Problem(PathOf(l.gameObject) + ": locked at start but nothing ever unlocks it (never interactable).");
        }

        foreach (InspectDialogue d in Object.FindObjectsByType<InspectDialogue>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!d.HasDialogue) Problem(PathOf(d.gameObject) + ": Inspect Dialogue has no dialogue.");
            if (d.GetComponent<Collider>() == null && d.GetComponentInChildren<Collider>(true) == null)
                Problem(PathOf(d.gameObject) + ": Inspect Dialogue has no collider (can't be found).");
            if (!d.gameObject.activeInHierarchy && !referenced.Contains(d.gameObject))
                Problem(PathOf(d.gameObject) + ": turned off and nothing turns it on.");
        }

        foreach (NPC npc in Object.FindObjectsByType<NPC>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (new SerializedObject(npc).FindProperty("startingNode") is SerializedProperty sp && sp.objectReferenceValue == null)
                notes.AppendLine("- " + PathOf(npc.gameObject) + ": NPC has no starting dialogue (fine if a sequence sets it later).");

        // Dialogue assets.
        foreach (string guid in AssetDatabase.FindAssets("t:DialogueNode"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var node = AssetDatabase.LoadAssetAtPath<DialogueNode>(path);
            if (node == null) continue;
            var so = new SerializedObject(node);
            SerializedProperty text = so.FindProperty("text");
            if (text != null && string.IsNullOrWhiteSpace(text.stringValue))
                notes.AppendLine("- " + path + ": empty text.");
            SerializedProperty choices = so.FindProperty("choices");
            if (choices != null)
                for (int i = 0; i < choices.arraySize; i++)
                    if (string.IsNullOrWhiteSpace(choices.GetArrayElementAtIndex(i).FindPropertyRelative("choiceText").stringValue))
                        Problem(path + ": choice " + (i + 1) + " has no text.");
        }

        // Testing leftovers.
        StoryPhases phases = Object.FindFirstObjectByType<StoryPhases>(FindObjectsInactive.Include);
        if (phases != null && phases.StartingPhase != StoryPhase.StartOfGame)
            Problem("Story Phases > Starting Phase is '" + phases.StartingPhase + "' (testing). Set it to 'Start of the game' before building.");
        IntroSequence intro = Object.FindFirstObjectByType<IntroSequence>(FindObjectsInactive.Include);
        if (intro != null)
        {
            var introSO = new SerializedObject(intro);
            foreach (string flag in new[] { "skipIntro", "skipEverything" })
            {
                SerializedProperty f = introSO.FindProperty(flag);
                if (f != null && f.propertyType == SerializedPropertyType.Boolean && f.boolValue)
                    Problem("Intro Sequence > '" + f.displayName + "' is ticked (testing).");
            }
        }

        foreach (EditorBuildSettingsScene s in EditorBuildSettings.scenes)
            if (s.enabled && !File.Exists(s.path)) Problem("Build Settings: scene not found: " + s.path);

        Directory.CreateDirectory("Temp");
        File.WriteAllText(ReportPath, "PROBLEMS (" + count + ")\n" + problems + "\nNOTES (probably fine)\n" + notes);
        EditorUtility.DisplayDialog("Check Everything", count + " problem(s) found.\nFull list: " + ReportPath, "OK");
    }

    private static string PathOf(GameObject go)
    {
        string p = go.name;
        for (Transform t = go.transform.parent; t != null; t = t.parent) p = t.name + "/" + p;
        return p;
    }
}
