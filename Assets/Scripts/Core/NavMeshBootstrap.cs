using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;

public class NavMeshBootstrap : MonoBehaviour
{
    [SerializeField] private bool _buildOnStart = true;

    private void Awake()
    {
        if (_buildOnStart)
            BuildNavMesh();
    }

    public void BuildNavMesh()
    {
        NavMesh.RemoveAllNavMeshData();

        var ourSources = new List<NavMeshBuildSource>();
        var enemySources = new List<NavMeshBuildSource>();
        var filters = FindObjectsOfType<MeshFilter>();
        foreach (var mf in filters)
        {
            if (!mf.gameObject.isStatic) continue;
            if (mf.sharedMesh == null) continue;
            if (mf.GetComponentInParent<Base>() != null) continue;

            var src = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Mesh,
                sourceObject = mf.sharedMesh,
                transform = mf.transform.localToWorldMatrix,
                area = 0
            };
            float srcX = mf.transform.position.x;
            if (srcX < 50f)
            {
                src.area = 0; // Walkable, our board
                ourSources.Add(src);
            }
            else
            {
                src.area = 3; // User area 3, enemy board
                enemySources.Add(src);
            }
        }

        if (ourSources.Count == 0 && enemySources.Count == 0)
        {
            Debug.LogError("[NavMeshBootstrap] No static meshes found for NavMesh baking.");
            return;
        }

        var settings = NavMesh.GetSettingsByID(0);
        settings.agentRadius = 0.3f;
        settings.agentHeight = 1f;
        settings.agentSlope = 45f;
        settings.agentClimb = 0.4f;
        settings.minRegionArea = 1f;

        var gm = GridManager.Instance;
        var halfSize = new Vector3(11f, 0.5f, 11f);

        var ourCenter = gm.GetBoardCenter(true);
        ourCenter.y = 0.1f;
        var ourBounds = new Bounds(ourCenter, halfSize);
        var ourData = NavMeshBuilder.BuildNavMeshData(settings, ourSources, ourBounds, Vector3.zero, Quaternion.identity);
        if (ourData != null)
        {
            NavMesh.AddNavMeshData(ourData);
            Debug.Log($"[NavMeshBootstrap] Our board NavMesh built (bounds: {ourBounds.center}, size: {ourBounds.size})");
        }
        else
            Debug.LogError("[NavMeshBootstrap] Our board NavMesh build failed.");

        var enemyCenter = gm.GetBoardCenter(false);
        enemyCenter.y = 0.1f;
        var enemyBounds = new Bounds(enemyCenter, halfSize);
        var enemyData = NavMeshBuilder.BuildNavMeshData(settings, enemySources, enemyBounds, Vector3.zero, Quaternion.identity);
        if (enemyData != null)
        {
            NavMesh.AddNavMeshData(enemyData);
            Debug.Log($"[NavMeshBootstrap] Enemy board NavMesh built (bounds: {enemyBounds.center}, size: {enemyBounds.size})");
        }
        else
            Debug.LogError("[NavMeshBootstrap] Enemy board NavMesh build failed.");

        var tri = NavMesh.CalculateTriangulation();
        Debug.Log($"[NavMeshBootstrap] Total NavMesh: {tri.vertices.Length} vertices, {tri.indices.Length / 3} triangles");
    }
}
