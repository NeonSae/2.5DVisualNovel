using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Sleepover > Setup Dinner (Kitchen)
// - kitchen door: not interactable during Dad's welcome, then until Stella leaves after dinner
// - after Dad's welcome: the kitchen table says "[E] Sit down"
// - sitting: black screen -> cinematic -> after-dinner talk -> Stella goes to her room and sits facing the TV
// - Dad goes to his cleaning spot and can be talked to
// Safe to run again: points you moved and dialogues you filled are kept.
public static class DinnerSetup
{
    private const string DoorOpeningGuid = "40cd69781a7c80f46b684d069ab090e2";
    private const string EatingGuid = "027aa064a143f474cb2b93c9364fe93b";    // 3. Kitchen/Eating/Eating 1
    private const string LetsGoGuid = "c06d2b53bfad0e44aad751f9031ea698";    // 3. Kitchen/AfterFood/Let's go
    private const string DadTalkGuid = "e9a8c8b831df4854ba16235bc5d37bc5";   // 3. Kitchen/AfterFood/Dad Interaction

    [MenuItem("Tools/Sleepover/Setup Dinner (Kitchen)")]
    public static void Setup()
    {
        var report = new List<string>();

        RoomEntranceScene entrance = Object.FindFirstObjectByType<RoomEntranceScene>(FindObjectsInactive.Include);
        IntroSequence intro = Object.FindFirstObjectByType<IntroSequence>(FindObjectsInactive.Include);
        CharacterMovement player = Object.FindFirstObjectByType<CharacterMovement>(FindObjectsInactive.Include);
        NpcMovement stella = FindNpc("Stella");
        NpcMovement dad = FindNpc("Dad");
        RoomDoor kitchenExit = FindDoor("Kitchen TO Corridor1");
        RoomDoor intoKitchen = FindDoor("Corridor1 TO Kitchen");
        RoomDoor c1ToC2 = FindDoor("Corridor1 TO Corridor2");
        RoomDoor c2ToStella = FindDoor("Corridor2 TO Stella's room");
        if (entrance == null || player == null || stella == null || dad == null ||
            kitchenExit == null || intoKitchen == null || c1ToC2 == null || c2ToStella == null ||
            kitchenExit.Destination == null || intoKitchen.Destination == null ||
            c1ToC2.Destination == null || c2ToStella.Destination == null)
        {
            EditorUtility.DisplayDialog("Setup Dinner",
                "Need: Kitchen Entrance Scene, the player, Stella, Dad and the doors 'Kitchen TO Corridor1', " +
                "'Corridor1 TO Kitchen', 'Corridor1 TO Corridor2', 'Corridor2 TO Stella's room'. Run 'Setup Kitchen Scene (Dad)' first.", "OK");
            return;
        }

        // ---------- Dinner Sequence object ----------
        DinnerSequence dinner = Object.FindFirstObjectByType<DinnerSequence>(FindObjectsInactive.Include);
        if (dinner == null)
        {
            var go = new GameObject("Dinner Sequence === EVENT ===");
            Undo.RegisterCreatedObjectUndo(go, "Create Dinner Sequence");
            dinner = go.AddComponent<DinnerSequence>();
            report.Add("Created 'Dinner Sequence === EVENT ==='.");
        }

        // ---------- Kitchen door lock (open at start of the game) ----------
        InteractionLock doorLock = kitchenExit.GetComponent<InteractionLock>();
        if (doorLock == null)
        {
            doorLock = Undo.AddComponent<InteractionLock>(kitchenExit.gameObject);
            var lockSO = new SerializedObject(doorLock);
            lockSO.FindProperty("lockedAtStart").boolValue = false;
            lockSO.ApplyModifiedProperties();
        }

        var entranceSO = new SerializedObject(entrance);
        SetArray(entranceSO.FindProperty("lockDuringScene"), doorLock);
        entranceSO.ApplyModifiedProperties();
        AddListenerOnce(GetEvent(entrance, "onFinished"), dinner, "Begin", dinner.Begin);
        report.Add("Kitchen door: locked when you come in, opens once Stella has left after dinner.");

        // ---------- Table "Sit down" ----------
        Transform table = FindKitchenTable(out Bounds tableBounds);
        if (table == null)
        {
            EditorUtility.DisplayDialog("Setup Dinner", "No 'Table' found inside the Kitchen.", "OK");
            return;
        }

        if (table.GetComponentInChildren<Collider>() == null)
        {
            BoxCollider box = Undo.AddComponent<BoxCollider>(table.gameObject);
            box.center = table.InverseTransformPoint(tableBounds.center);
            box.size = Abs(table.InverseTransformVector(tableBounds.size));
        }

        SitDown sit = table.GetComponent<SitDown>();
        if (sit == null) sit = Undo.AddComponent<SitDown>(table.gameObject);
        InteractionLock sitLock = table.GetComponent<InteractionLock>();
        if (sitLock == null) sitLock = Undo.AddComponent<InteractionLock>(table.gameObject);   // locked at start
        AddListenerOnce(GetEvent(sit, "onSit"), dinner, "Sit", dinner.Sit);
        report.Add("'" + table.name + "' in the Kitchen: '[E] Sit down' after Dad's welcome.");

        // ---------- Points ----------
        float kitchenFloor = intoKitchen.Destination.position.y;
        Vector3 c = tableBounds.center; c.y = kitchenFloor;
        Transform tableCenter = GetOrCreatePoint("DadLookAt_Table", c, report);
        Transform playerSeat = GetOrCreatePoint("DinnerSeat_Nora", new Vector3(c.x, kitchenFloor, tableBounds.min.z - 0.4f), report);
        Transform stellaSeat = GetOrCreatePoint("DinnerSeat_Stella", new Vector3(tableBounds.min.x - 0.4f, kitchenFloor, c.z), report);
        Transform dadSeat = GetOrCreatePoint("DinnerSeat_Dad", new Vector3(tableBounds.max.x + 0.4f, kitchenFloor, c.z), report);
        Transform dadCleaning = GetOrCreatePoint("DadPoint_Cleaning", new Vector3(tableBounds.max.x + 1.5f, kitchenFloor, tableBounds.max.z + 0.5f), report);

        Transform stellaExit = GetOrCreatePoint("StellaPoint_KitchenExit",
            NearDoor(kitchenExit, intoKitchen.Destination.position, kitchenFloor), report);
        // Route to her room: Corridor 1 -> Corridor 2 -> Stella's room (reuses her first route's points).
        Transform c1Point = GetOrCreatePoint("StellaPoint_Corridor1",
            NearDoor(c1ToC2, kitchenExit.Destination.position, kitchenExit.Destination.position.y), report);
        Transform c2Point = GetOrCreatePoint("StellaPoint_Corridor2",
            NearDoor(c2ToStella, c1ToC2.Destination.position, c1ToC2.Destination.position.y), report);

        // Seat & TV in Stella's room: a couch if there is one, else her bed.
        float roomFloor = c2ToStella.Destination.position.y;
        Transform couch = FindInRoom("Couch", "Stella", out Bounds couchBounds);
        if (couch == null) couch = FindInRoom("Sofa", "Stella", out couchBounds);
        if (couch == null) couch = FindInRoom("Bed", "Stella", out couchBounds);
        Transform tv = FindInRoom("TV", "Stella", out Bounds tvBounds);
        Vector3 seatPos = couch != null
            ? new Vector3(couchBounds.center.x, couchBounds.min.y + couchBounds.size.y * 0.35f, couchBounds.center.z)
            : c2ToStella.Destination.position + Vector3.forward * 2f;
        Vector3 towardTv = tv != null ? tvBounds.center - seatPos : Vector3.forward;
        towardTv.y = 0f;
        towardTv = towardTv.sqrMagnitude > 0.01f ? towardTv.normalized : Vector3.forward;
        Vector3 frontPos = seatPos + towardTv * (couch != null ? Mathf.Max(couchBounds.extents.x, couchBounds.extents.z) * 0.6f + 0.4f : 0.8f);
        frontPos.y = roomFloor;

        // These two used to be in the living room: moved to Stella's room if they're still far away.
        Transform couchSeat = PlacePoint("StellaPoint_CouchSeat", seatPos);
        Transform couchFront = PlacePoint("StellaPoint_CouchFront", frontPos);
        report.Add(couch != null
            ? "Stella sits on '" + couch.name + "' in her room (StellaPoint_CouchSeat / _CouchFront placed there)."
            : "! No couch/bed found in Stella's room: move 'StellaPoint_CouchSeat' / '_CouchFront'.");
        if (tv == null) report.Add("! No TV found in Stella's room: drag it into 'Tv' on Dinner Sequence.");

        // The old living room stop isn't used anymore.
        GameObject oldPoint = GameObject.Find("StellaPoint_Corridor1_ToLivingRoom");
        if (oldPoint != null) Undo.DestroyObjectImmediate(oldPoint);

        // ---------- Dad: talkable after dinner ----------
        if (dad.GetComponent<Collider>() == null)
        {
            var capsule = Undo.AddComponent<CapsuleCollider>(dad.gameObject);
            capsule.center = new Vector3(0f, 0.5f, 0f);
            capsule.radius = 0.3f;
            capsule.height = 1f;
        }
        if (dad.GetComponent<InteractionLock>() == null)
            Undo.AddComponent<InteractionLock>(dad.gameObject);   // locked until after dinner
        NPC dadTalk = dad.GetComponent<NPC>();
        if (dadTalk == null)
        {
            dadTalk = Undo.AddComponent<NPC>(dad.gameObject);
            var talkSO = new SerializedObject(dadTalk);
            talkSO.FindProperty("talkOnlyOnce").boolValue = false;
            talkSO.ApplyModifiedProperties();
        }

        // ---------- Fill the Dinner Sequence ----------
        var so = new SerializedObject(dinner);
        SetArray(so.FindProperty("lockedUntilStellaLeaves"), doorLock);
        so.FindProperty("sitLock").objectReferenceValue = sitLock;
        if (intro != null)
        {
            var introSO = new SerializedObject(intro);
            so.FindProperty("screenFader").objectReferenceValue = introSO.FindProperty("screenFader").objectReferenceValue;
            so.FindProperty("dialogueCanvas").objectReferenceValue = introSO.FindProperty("dialogueCanvas").objectReferenceValue;
            so.FindProperty("dialogueManager").objectReferenceValue = introSO.FindProperty("dialogueManager").objectReferenceValue;
        }
        PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
        if (interactor == null) interactor = Object.FindFirstObjectByType<PlayerInteractor>(FindObjectsInactive.Include);
        SetArray(so.FindProperty("disableDuringMeal"), player, interactor);

        so.FindProperty("playerSeat").objectReferenceValue = playerSeat;
        so.FindProperty("stella").objectReferenceValue = stella;
        so.FindProperty("stellaSeat").objectReferenceValue = stellaSeat;
        so.FindProperty("dad").objectReferenceValue = dad;
        so.FindProperty("dadSeat").objectReferenceValue = dadSeat;
        so.FindProperty("tableCenter").objectReferenceValue = tableCenter;

        SerializedProperty route = so.FindProperty("stellaRoute");
        route.arraySize = 3;
        SetStep(route.GetArrayElementAtIndex(0), stellaExit, kitchenExit);
        SetStep(route.GetArrayElementAtIndex(1), c1Point, c1ToC2);
        SetStep(route.GetArrayElementAtIndex(2), c2Point, c2ToStella);
        so.FindProperty("couchFront").objectReferenceValue = couchFront;
        so.FindProperty("couchSeat").objectReferenceValue = couchSeat;
        if (tv != null) so.FindProperty("tv").objectReferenceValue = tv;
        so.FindProperty("doorSound").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(DoorOpeningGuid));

        so.FindProperty("dadCleaningPoint").objectReferenceValue = dadCleaning;
        so.FindProperty("dadTalk").objectReferenceValue = dadTalk;
        so.ApplyModifiedProperties();

        // Your dialogues (3. Kitchen), only put in empty slots.
        FillIfEmpty(so, "cinematicDialogue", EatingGuid, report, "Cinematic = Eating 1.");
        FillIfEmpty(so, "afterDinnerDialogue", LetsGoGuid, report, "After dinner = AfterFood/Let's go.");
        FillIfEmpty(so, "dadTalkDialogue", DadTalkGuid, report, "Dad's talk = AfterFood/Dad Interaction.");
        so.ApplyModifiedProperties();

        if (so.FindProperty("cinematicDialogue").objectReferenceValue == null)
            report.Add("TO DO: 'Cinematic Dialogue' = first line of the dinner cutscene (custscene2 + its music).");
        if (so.FindProperty("afterDinnerDialogue").objectReferenceValue == null)
            report.Add("TO DO: 'After Dinner Dialogue' = Dad cleans up / Stella: movie in her room.");
        if (so.FindProperty("dadTalkDialogue").objectReferenceValue == null)
            report.Add("TO DO: 'Dad Talk Dialogue' = what Dad says when you talk to him after dinner.");

        Selection.activeGameObject = dinner.gameObject;
        EditorSceneManager.MarkSceneDirty(dinner.gameObject.scene);
        EditorSceneManager.SaveScene(dinner.gameObject.scene);
        report.Add("Scene saved.");

        EditorUtility.DisplayDialog("Setup Dinner", "Done!\n\n- " + string.Join("\n- ", report), "OK");
    }

    // ---------------- Helpers ----------------

    private static UnityEngine.Events.UnityEvent GetEvent(object target, string field)
    {
        return (UnityEngine.Events.UnityEvent)target.GetType()
            .GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .GetValue(target);
    }

    // Adds target.method to the event unless it's already there (keeps the user's other listeners).
    private static void AddListenerOnce(UnityEngine.Events.UnityEvent evt, Object target, string method, UnityEngine.Events.UnityAction call)
    {
        for (int i = 0; i < evt.GetPersistentEventCount(); i++)
            if (evt.GetPersistentTarget(i) == target && evt.GetPersistentMethodName(i) == method)
                return;
        UnityEventTools.AddPersistentListener(evt, call);
    }

    private static void FillIfEmpty(SerializedObject so, string field, string guid, List<string> report, string label)
    {
        SerializedProperty prop = so.FindProperty(field);
        if (prop.objectReferenceValue != null)
            return;

        DialogueNode node = AssetDatabase.LoadAssetAtPath<DialogueNode>(AssetDatabase.GUIDToAssetPath(guid));
        if (node == null)
            return;

        prop.objectReferenceValue = node;
        report.Add(label);
    }

    private static void SetStep(SerializedProperty step, Transform walkTo, RoomDoor door)
    {
        step.FindPropertyRelative("walkTo").objectReferenceValue = walkTo;
        step.FindPropertyRelative("door").objectReferenceValue = door;
    }

    // Like GetOrCreatePoint, but moves it there if it's far away (e.g. still in another room).
    // A point you moved a little stays where you put it.
    private static Transform PlacePoint(string name, Vector3 position)
    {
        Transform point = GetOrCreatePoint(name, position, new List<string>());
        if (Vector3.Distance(point.position, position) > 4f)
        {
            Undo.RecordObject(point, "Move point");
            point.position = position;
        }
        return point;
    }

    private static void SetArray(SerializedProperty array, params Object[] values)
    {
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

    private static NpcMovement FindNpc(string name)
    {
        foreach (NpcMovement npc in Object.FindObjectsByType<NpcMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (npc.name == name) return npc;
        return null;
    }

    // The biggest "Table" inside the Kitchen.
    private static Transform FindKitchenTable(out Bounds bounds)
    {
        return FindInRoom("Table", "Kitchen", out bounds);
    }

    // The biggest object whose name contains 'part' under a room whose name contains 'room'.
    private static Transform FindInRoom(string part, string room, out Bounds bounds)
    {
        Transform best = null;
        bounds = default;
        float bestSize = 0f;

        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!t.name.ToLower().Contains(part.ToLower()) || !HasAncestorContaining(t, room))
                continue;

            Renderer[] renderers = t.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) continue;
            Bounds b = renderers[0].bounds;
            foreach (Renderer r in renderers) b.Encapsulate(r.bounds);

            float size = b.size.x * b.size.z;
            if (size > bestSize) { bestSize = size; best = t; bounds = b; }
        }
        return best;
    }

    private static bool HasAncestorContaining(Transform t, string name)
    {
        for (Transform p = t.parent; p != null; p = p.parent)
            if (p.name.ToLower().Contains(name.ToLower()) && !p.name.Contains(" TO ")) return true;
        return false;
    }

    private static Vector3 NearDoor(RoomDoor door, Vector3 comingFrom, float floorY)
    {
        Vector3 doorPos = door.transform.position;
        Vector3 toward = comingFrom - doorPos;
        toward.y = 0f;
        Vector3 point = doorPos + (toward.sqrMagnitude > 0.01f ? toward.normalized : Vector3.back);
        point.y = floorY;
        return point;
    }

    private static Transform GetOrCreatePoint(string name, Vector3 position, List<string> report)
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == name && t.gameObject.scene.IsValid())
                return t;

        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create point");
        go.transform.position = position;
        report.Add("Created '" + name + "' (check its position).");
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
