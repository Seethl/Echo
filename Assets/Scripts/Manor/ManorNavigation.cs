using System.Collections.Generic;
using Echo.Interactions;
using UnityEngine;
using UnityEngine.AI;

namespace Echo.Manor
{
    // Uses the built-in navigation module; no additional AI Navigation package.
    [DefaultExecutionOrder(2000)]
    public sealed class ManorNavigation : MonoBehaviour
    {
        public Bounds worldBounds = new Bounds(new Vector3(9, 3, 18), new Vector3(72, 14, 76));
        public bool Ready { get; private set; }
        private NavMeshData data;
        private NavMeshDataInstance instance;

        private void Start()
        {
            var exclusions = new List<NavMeshBuildMarkup>();
            foreach (var player in FindObjectsByType<FPSController>())
                exclusions.Add(new NavMeshBuildMarkup {root=player.transform, ignoreFromBuild=true});
            foreach (var enemy in FindObjectsByType<EnemyInvestigator>())
                exclusions.Add(new NavMeshBuildMarkup {root=enemy.transform, ignoreFromBuild=true});
            foreach (var item in FindObjectsByType<Interactable>(FindObjectsInactive.Include))
                exclusions.Add(new NavMeshBuildMarkup {root=item.transform, ignoreFromBuild=true});
            foreach (var blocker in FindObjectsByType<NavMeshObstacle>(FindObjectsInactive.Include))
                exclusions.Add(new NavMeshBuildMarkup {root=blocker.transform, ignoreFromBuild=true});
            foreach (var zone in FindObjectsByType<ManorZone>())
                if (zone.tutorialSafe) exclusions.Add(new NavMeshBuildMarkup {root=zone.transform, ignoreFromBuild=true});
            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(worldBounds, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, exclusions, sources);
            var settings = NavMesh.GetSettingsByID(0);
            settings.agentRadius = 0.35f; settings.agentHeight = 1.8f;
            settings.agentClimb = 0.3f; settings.overrideVoxelSize = true; settings.voxelSize = 0.12f;
            data = NavMeshBuilder.BuildNavMeshData(settings, sources, worldBounds, Vector3.zero, Quaternion.identity);
            if (data == null) { Debug.LogError("Manor navigation build failed.", this); return; }
            instance = NavMesh.AddNavMeshData(data);
            foreach (var agent in FindObjectsByType<NavMeshAgent>(FindObjectsInactive.Include))
            {
                if (!NavMesh.SamplePosition(agent.transform.position, out var hit, 1f, NavMesh.AllAreas))
                { Debug.LogError("Enemy has no navigation beneath its spawn.", agent); continue; }
                agent.transform.position = hit.position; agent.enabled = true;
                agent.Warp(hit.position);
            }
            Ready = true;
        }

        private void OnDestroy()
        {
            if (instance.valid) instance.Remove();
            if (data != null) Destroy(data);
        }
    }
}
