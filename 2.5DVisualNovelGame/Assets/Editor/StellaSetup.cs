using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Sleepover > Setup Stella & Corridor Door
// - fixes Stella's sprite import settings (pixel art, same size as Nora)
// - creates Stella next to the player with her walk frames
// - locks the "Living room TO Corridor1" door until the first RPG dialogue is over
// - after that dialogue: Stella walks to the door and follows Nora through doors
// Safe to run again (e.g. after adding stella walk14.png): it updates instead of duplicating.
public static class StellaSetup
{
    private const string StellaName = "Stella";
    private const string DoorPointName = "StellaDoorPoint";
    private const string CorridorDoorName = "Living room TO Corridor1";

    [MenuItem("Tools/Sleepover/Setup Stella & Corridor Door")]
    public static void Setup()
    {
        var report = new List<string>();

        // ---------- Sprites ----------
        Sprite[] frames = LoadStellaFrames(report);

        // ---------- Player reference ----------
        CharacterMovement player = Object.FindFirstObjectByType<CharacterMovement>(FindObjectsInactive.Include);
        if (player == null)
        {
            EditorUtility.DisplayDialog("Setup Stella", "No player (CharacterMovement) in the open scene. Open SampleScene first.", "OK");
            return;
        }

        SpriteRenderer noraRenderer = new SerializedObject(player).FindProperty("spriteRenderer").objectReferenceValue as SpriteRenderer;

        // ---------- Stella object ----------
        GameObject stella = FindInScene(StellaName);
        bool created = stella == null;
        if (created)
        {
            stella = new GameObject(StellaName);
            Undo.RegisterCreatedObjectUndo(stella, "Setup Stella");
            stella.transform.position = player.transform.position + player.transform.right * 1.2f;
            report.Add("Created Stella next to Nora (move her where you want).");
        }

        // Visual child, sized and placed like Nora's.
        Transform visual = stella.transform.Find("Visual");
        if (visual == null)
        {
            visual = new GameObject("Visual").transform;
            visual.SetParent(stella.transform, false);
            if (noraRenderer != null)
            {
                visual.localPosition = noraRenderer.transform.localPosition;
                visual.localRotation = noraRenderer.transform.localRotation;
                visual.localScale = noraRenderer.transform.localScale;
            }
        }

        SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
        if (renderer == null) renderer = visual.gameObject.AddComponent<SpriteRenderer>();
        if (noraRenderer != null) renderer.sharedMaterial = noraRenderer.sharedMaterial;
        if (frames[0] != null) renderer.sprite = frames[0];

        // Same blob/shadow setup as Nora, if she has one.
        Transform noraShadows = player.transform.Find("Shadows");
        if (noraShadows != null && stella.transform.Find("Shadows") == null)
        {
            GameObject shadows = Object.Instantiate(noraShadows.gameObject, stella.transform);
            shadows.name = "Shadows";
            shadows.transform.localPosition = noraShadows.localPosition;
            shadows.transform.localRotation = noraShadows.localRotation;
            shadows.transform.localScale = noraShadows.localScale;
        }

        // Movement + animation.
        NpcMovement movement = stella.GetComponent<NpcMovement>();
        if (movement == null) movement = stella.AddComponent<NpcMovement>();
        var moveSO = new SerializedObject(movement);
        moveSO.FindProperty("spriteRenderer").objectReferenceValue = renderer;
        SetFrames(moveSO.FindProperty("walkFront"), frames, 1);
        SetFrames(moveSO.FindProperty("walkFrontRight"), frames, 5);
        SetFrames(moveSO.FindProperty("walkRight"), frames, 9);
        SetFrames(moveSO.FindProperty("walkBackRight"), frames, 13);
        SetFrames(moveSO.FindProperty("walkBack"), frames, 17);
        moveSO.ApplyModifiedProperties();

        DoorTraveler traveler = stella.GetComponent<DoorTraveler>();
        if (traveler == null) traveler = stella.AddComponent<DoorTraveler>();
        report.Add("Stella: walk animation + follows through doors (after the dialogue).");

        // ---------- Corridor door ----------
        RoomDoor door = FindDoor(CorridorDoorName);
        InteractionLock doorLock = null;
        Transform doorPoint = null;

        if (door == null)
        {
            report.Add("! Door '" + CorridorDoorName + "' not found: lock it yourself (add Interaction Lock).");
        }
        else
        {
            doorLock = door.GetComponent<InteractionLock>();
            if (doorLock == null) doorLock = Undo.AddComponent<InteractionLock>(door.gameObject);
            report.Add("Corridor door locked until Stella has talked.");

            GameObject pointGO = FindInScene(DoorPointName);
            if (pointGO == null)
            {
                pointGO = new GameObject(DoorPointName);
                Undo.RegisterCreatedObjectUndo(pointGO, "Setup Stella");

                // 1 unit in front of the door, toward where Nora starts.
                Vector3 doorPos = door.transform.position;
                Vector3 toPlayer = player.transform.position - doorPos;
                toPlayer.y = 0f;
                Vector3 point = doorPos + (toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : Vector3.back) * 1f;
                point.y = stella.transform.position.y;
                pointGO.transform.position = point;
                report.Add("Created 'StellaDoorPoint' near the door (move it if needed).");
            }
            doorPoint = pointGO.transform;
        }

        // ---------- Intro Sequence ----------
        IntroSequence intro = Object.FindFirstObjectByType<IntroSequence>(FindObjectsInactive.Include);
        if (intro != null)
        {
            var so = new SerializedObject(intro);
            SetArray(so.FindProperty("unlockAfterFollowUp"), doorLock);
            so.FindProperty("npcToMove").objectReferenceValue = movement;
            so.FindProperty("npcDestination").objectReferenceValue = doorPoint;
            SetArray(so.FindProperty("startFollowingPlayer"), traveler);
            so.FindProperty("delayAfterCutscene").floatValue = 3f;
            so.ApplyModifiedProperties();
            report.Add("Intro: after the choice, door unlocks + Stella walks to it.");
        }
        else
        {
            report.Add("! No Intro Sequence found.");
        }

        // Last cutscene picture = slowest fade.
        foreach (DialogueUI ui in Object.FindObjectsByType<DialogueUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var uiSO = new SerializedObject(ui);
            uiSO.FindProperty("cutsceneEndFadeDuration").floatValue = 2.5f;
            uiSO.ApplyModifiedProperties();
        }
        report.Add("Last cutscene picture fades out over 2.5s.");

        EditorSceneManager.MarkSceneDirty(stella.scene);
        EditorSceneManager.SaveScene(stella.scene);
        report.Add("Scene saved.");

        Selection.activeGameObject = stella;
        EditorUtility.DisplayDialog("Setup Stella", "Done!\n\n- " + string.Join("\n- ", report), "OK");
    }

    // ---------------- Helpers ----------------

    // Returns frames[1..20] (index 0 = first available frame), fixing import settings on the way.
    private static Sprite[] LoadStellaFrames(List<string> report)
    {
        var result = new Sprite[21];
        var regex = new Regex(@"^stella walk(\d+)$", RegexOptions.IgnoreCase);
        int found = 0;

        foreach (string guid in AssetDatabase.FindAssets("stella walk t:Texture2D"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Match match = regex.Match(System.IO.Path.GetFileNameWithoutExtension(path));
            if (!match.Success)
                continue;

            int number = int.Parse(match.Groups[1].Value);
            if (number < 1 || number > 20)
                continue;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);

                bool needsFix =
                    importer.textureType != TextureImporterType.Sprite ||
                    importer.spriteImportMode != SpriteImportMode.Single ||
                    !Mathf.Approximately(importer.spritePixelsPerUnit, 32f) ||
                    importer.filterMode != FilterMode.Point ||
                    settings.spriteAlignment != (int)SpriteAlignment.BottomCenter;

                if (needsFix)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.spritePixelsPerUnit = 32f;
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.alphaIsTransparency = true;

                    // Feet on the floor, like Nora (Bottom pivot).
                    importer.ReadTextureSettings(settings);
                    settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                    importer.SetTextureSettings(settings);

                    importer.SaveAndReimport();
                }
            }

            result[number] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            found++;
        }

        for (int i = 1; i <= 20; i++)
        {
            if (result[i] != null) { result[0] = result[i]; break; }
        }

        report.Add("Stella frames found: " + found + "/20 (pixel-art import settings fixed).");
        return result;
    }

    private static void SetFrames(SerializedProperty array, Sprite[] frames, int first)
    {
        array.arraySize = 4;
        for (int i = 0; i < 4; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = frames[first + i];
    }

    private static void SetArray(SerializedProperty array, params Object[] values)
    {
        var list = new List<Object>();
        foreach (Object value in values)
            if (value != null) list.Add(value);

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
