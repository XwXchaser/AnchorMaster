using UnityEngine;

public class MapLoader : MonoBehaviour
{
    [SerializeField] private MapConfig _mapConfig;

    private void Start()
    {
        if (_mapConfig == null)
        {
            Debug.Log("[MapLoader] No MapConfig assigned, using default positions.");
            return;
        }

        ApplyConfig();
    }

    private void ApplyConfig()
    {
        GridManager gm = GridManager.Instance;
        if (gm == null) return;

        Debug.Log($"[MapLoader] Loading map config...");

        // Move bases
        var bases = FindObjectsOfType<Base>();
        foreach (var b in bases)
        {
            if (b.IsOurBase)
            {
                b.transform.position = gm.GridToWorld(_mapConfig.OurBaseGridPos, true);
                Debug.Log($"[MapLoader] OurBase -> {_mapConfig.OurBaseGridPos}");
            }
            else
            {
                b.transform.position = gm.GridToWorld(_mapConfig.EnemyBaseGridPos, false);
                Debug.Log($"[MapLoader] EnemyBase -> {_mapConfig.EnemyBaseGridPos}");
            }
        }

        // Move portals
        if (PortalManager.Instance != null)
        {
            var pm = PortalManager.Instance;
            pm.OurPortalOurSide?.MoveTo(_mapConfig.OurPortalOurSidePos);
            pm.OurPortalEnemySide?.MoveTo(_mapConfig.OurPortalEnemySidePos);
            pm.EnemyPortalEnemySide?.MoveTo(_mapConfig.EnemyPortalEnemySidePos);
            pm.EnemyPortalOurSide?.MoveTo(_mapConfig.EnemyPortalOurSidePos);
            Debug.Log("[MapLoader] Portals repositioned.");
        }

        // Place obstacles
        if (ObstacleManager.Instance != null)
        {
            var om = ObstacleManager.Instance;
            foreach (var pos in _mapConfig.OurObstacles)
                om.PlaceObstacle(pos, true);
            foreach (var pos in _mapConfig.EnemyObstacles)
                om.PlaceObstacle(pos, false);
            Debug.Log($"[MapLoader] Placed {_mapConfig.OurObstacles.Count + _mapConfig.EnemyObstacles.Count} obstacles.");
        }
    }
}
