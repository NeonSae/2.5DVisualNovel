using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Builds the walkable area (NavMesh) for NPCs automatically when the scene starts.
// No setup: NpcMovement asks for it. Walls and furniture with colliders become obstacles.
public class RuntimeNavMesh : MonoBehaviour
{
    public static bool IsReady { get; private set; }

    // Small agent (like Nora): fits through doors, can't climb beds or tables.
    private const float AgentRadius = 0.2f;
    private const float AgentHeight = 1.0f;
    private const float MaxStep = 0.2f;
    private const float MaxSlope = 30f;

    private static RuntimeNavMesh instance;
    private NavMeshDataInstance dataInstance;

    public static void EnsureBuilt()
    {
        if (instance != null)
            return;

        instance = new GameObject("Runtime NavMesh").AddComponent<RuntimeNavMesh>();
    }

    private void Start()
    {
        IsReady = false;
        StartCoroutine(Build());
    }

    private IEnumerator Build()
    {
        // Everything with a collider, except triggers and the characters themselves.
        var sources = new List<NavMeshBuildSource>();
        var bounds = new Bounds(Vector3.zero, new Vector3(2000f, 200f, 2000f));
        NavMeshBuilder.CollectSources(bounds, ~0, NavMeshCollectGeometry.PhysicsColliders, 0,
            new List<NavMeshBuildMarkup>(), sources);

        sources.RemoveAll(source =>
        {
            Collider collider = source.component as Collider;
            if (collider == null) return false;
            return collider.isTrigger ||
                   collider is CharacterController ||
                   collider.GetComponentInParent<NpcMovement>() != null ||
                   collider.GetComponentInParent<CharacterMovement>() != null;
        });

        NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0);
        settings.agentRadius = AgentRadius;
        settings.agentHeight = AgentHeight;
        settings.agentClimb = MaxStep;
        settings.agentSlope = MaxSlope;
        settings.overrideVoxelSize = true;
        settings.voxelSize = 0.07f;

        var data = new NavMeshData(settings.agentTypeID);
        dataInstance = NavMesh.AddNavMeshData(data);

        AsyncOperation op = NavMeshBuilder.UpdateNavMeshDataAsync(data, settings, sources, bounds);
        yield return op;

        IsReady = true;
    }

    private void OnDestroy()
    {
        dataInstance.Remove();
        if (instance == this)
        {
            instance = null;
            IsReady = false;
        }
    }
}
