using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Sleepover > Colliders: Preview Cleanup / Remove Useless Colliders
// Finds colliders nobody needs (plates on tables, wall decorations, ceiling stuff, tiny props)
// and removes them. Keeps triggers, walls/floors, doors, interactables, characters and furniture you bump into.
// Preview first: it writes the list to Temp/ColliderReport.txt (nothing is changed).
public static class ColliderCleanup
{
    private const string ReportPath = "Temp/ColliderReport.txt";

    private static readonly string[] KeepNames =
        { "wall", "floor", "plane", "confin", "door", "stair", "bound", "zone", "invisible", "barrier", "block", "trigger", "portal" };

    [MenuItem("Tools/Sleepover/Colliders: Preview Cleanup")]
    public static void Preview() => Run(false);

    [MenuItem("Tools/Sleepover/Colliders: Remove Useless Colliders")]
    public static void Remove()
    {
        if (!EditorUtility.DisplayDialog("Colliders", "Remove the colliders listed as REMOVE in the preview?\n(Ctrl+Z undoes it.)", "Remove", "Cancel"))
            return;
        Run(true);
    }

    private static void Run(bool apply)
    {
        CharacterMovement player = Object.FindFirstObjectByType<CharacterMovement>(FindObjectsInactive.Include);
        if (player == null)
        {
            EditorUtility.DisplayDialog("Colliders", "No player in the open scene.", "OK");
            return;
        }

        Physics.SyncTransforms();
        float floorY = player.transform.position.y;
        CharacterController cc = player.GetComponent<CharacterController>();
        float noraTop = floorY + (cc != null ? cc.height * player.transform.lossyScale.y : 1.6f);

        var keep = new StringBuilder();
        var remove = new StringBuilder();
        var toRemove = new List<Collider>();

        foreach (Collider c in Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c is CharacterController) continue;
            string reason = WhyKeep(c);
            string whyRemove = reason == null ? WhyRemove(c, floorY, noraTop) : null;

            string line = Path(c.transform) + "  [" + c.GetType().Name + "]  size " + c.bounds.size.ToString("F2") +
                          "  bottom " + (c.bounds.min.y - floorY).ToString("F2");
            if (whyRemove != null)
            {
                remove.AppendLine("REMOVE (" + whyRemove + ")  " + line);
                toRemove.Add(c);
            }
            else
            {
                keep.AppendLine("KEEP (" + (reason ?? "blocks walking") + ")  " + line);
            }
        }

        Directory.CreateDirectory("Temp");
        File.WriteAllText(ReportPath, "Nora floor y = " + floorY.ToString("F2") + ", head = " + noraTop.ToString("F2") +
            "\n\n=== REMOVE (" + toRemove.Count + ") ===\n" + remove + "\n=== KEEP ===\n" + keep);

        if (!apply)
        {
            EditorUtility.DisplayDialog("Colliders", toRemove.Count + " colliders would be removed.\nList: " + ReportPath, "OK");
            return;
        }

        Undo.SetCurrentGroupName("Remove useless colliders");
        foreach (Collider c in toRemove)
            Undo.DestroyObjectImmediate(c);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        EditorSceneManager.SaveScene(player.gameObject.scene);
        EditorUtility.DisplayDialog("Colliders", "Removed " + toRemove.Count + " colliders. Scene saved.\n(Ctrl+Z to undo.)", "OK");
    }

    private static string WhyKeep(Collider c)
    {
        if (!c.enabled || !c.gameObject.activeInHierarchy) return "turned off, not checked";
        if (c.isTrigger) return "trigger";
        if (c.GetComponentInParent<Iinteractable.IInteractable>(true) != null) return "interactable";
        if (c.GetComponentInParent<UnityEngine.AI.NavMeshAgent>(true) != null) return "character";
        foreach (MonoBehaviour mb in c.GetComponents<MonoBehaviour>())
            if (mb != null && mb.GetType().Assembly.GetName().Name == "Assembly-CSharp") return "has a script (" + mb.GetType().Name + ")";
        for (Transform t = c.transform; t != null; t = t.parent)
        {
            string n = t.name.ToLower();
            foreach (string k in KeepNames)
                if (n.Contains(k)) return "name '" + k + "'";
        }
        return null;
    }

    private static string WhyRemove(Collider c, float floorY, float noraTop)
    {
        Bounds b = c.bounds;
        if (b.min.y >= noraTop) return "above Nora's head";
        if (Mathf.Max(b.size.x, b.size.y, b.size.z) < 0.2f) return "tiny";
        if (b.min.y > floorY + 0.4f && Mathf.Min(b.size.x, b.size.z) < 0.15f) return "flat on a wall";
        if (b.min.y > floorY + 0.15f && RestsOnSomething(c)) return "sits on furniture";
        return null;
    }

    // Something solid right under it (a plate on a table, a book on a shelf).
    private static bool RestsOnSomething(Collider c)
    {
        Bounds b = c.bounds;
        // Thin slab at the object's bottom: anything solid in it is what the object stands on.
        Vector3 center = new Vector3(b.center.x, b.min.y, b.center.z);
        Vector3 half = new Vector3(b.extents.x * 0.5f, 0.06f, b.extents.z * 0.5f);
        foreach (Collider other in Physics.OverlapBox(center, half, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            if (other != c && !other.transform.IsChildOf(c.transform) && !(other is CharacterController) &&
                other.GetComponentInParent<UnityEngine.AI.NavMeshAgent>() == null)
                return true;
        return false;
    }

    private static string Path(Transform t)
    {
        string p = t.name;
        while ((t = t.parent) != null) p = t.name + "/" + p;
        return p;
    }
}
