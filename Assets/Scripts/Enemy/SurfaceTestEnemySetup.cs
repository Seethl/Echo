using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Only for SurfaceTest: builds navigation and a visible placeholder at Play.</summary>
[RequireComponent(typeof(NoiseListener))]
public class SurfaceTestEnemySetup : MonoBehaviour
{
    private NavMeshDataInstance navigation;
    private NavMeshData data;
    private Material material;

    void Start()
    {
        var sources = new List<NavMeshBuildSource>();
        var exclusions = new List<NavMeshBuildMarkup>();
        foreach (var player in FindObjectsByType<FPSController>())
        {
            exclusions.Add(new NavMeshBuildMarkup { root = player.transform, ignoreFromBuild = true });
            if (player.GetComponent<PlayerCaught>() == null) player.gameObject.AddComponent<PlayerCaught>();
        }
        exclusions.Add(new NavMeshBuildMarkup { root = transform, ignoreFromBuild = true });
        var bounds = new Bounds(Vector3.zero, new Vector3(60f, 20f, 60f));
        NavMeshBuilder.CollectSources(bounds, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, exclusions, sources);
        data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0), sources, bounds, Vector3.zero, Quaternion.identity);
        if (data == null)
        {
            Debug.LogError("SurfaceTest navigation could not be built.", this);
            return;
        }
        navigation = NavMesh.AddNavMeshData(data);
        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit start, 3f, NavMesh.AllAreas))
        {
            Debug.LogError("No walkable floor under the test enemy.", this);
            return;
        }
        transform.position = start.position;
        var agent = gameObject.AddComponent<NavMeshAgent>();
        agent.radius = 0.35f;
        agent.height = 1.8f;
        agent.speed = 2.5f;
        agent.angularSpeed = 240f;
        agent.stoppingDistance = 0.7f;
        gameObject.AddComponent<EnemyInvestigator>();
        gameObject.AddComponent<EnemyCatchPlayer>();

        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "EnemyPlaceholder";
        body.transform.SetParent(transform, false);
        body.transform.localPosition = Vector3.up * 0.9f;
        body.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
        Destroy(body.GetComponent<Collider>());
        material = body.GetComponent<Renderer>().material;
        material.color = new Color(0.65f, 0.12f, 0.12f);
    }

    void OnDestroy()
    {
        if (navigation.valid) navigation.Remove();
        if (data != null) Destroy(data);
        if (material != null) Destroy(material);
    }
}
