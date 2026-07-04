using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Map_", menuName = "AnchorMaster/Map Config")]
public class MapConfig : ScriptableObject
{
    public Vector2Int OurBaseGridPos = new Vector2Int(0, 5);
    public Vector2Int EnemyBaseGridPos = new Vector2Int(9, 5);
    public Vector2Int OurPortalOurSidePos = new Vector2Int(4, 5);
    public Vector2Int OurPortalEnemySidePos = new Vector2Int(4, 5);
    public Vector2Int EnemyPortalEnemySidePos = new Vector2Int(5, 5);
    public Vector2Int EnemyPortalOurSidePos = new Vector2Int(5, 5);

    public List<Vector2Int> OurObstacles = new List<Vector2Int>();
    public List<Vector2Int> EnemyObstacles = new List<Vector2Int>();
}
