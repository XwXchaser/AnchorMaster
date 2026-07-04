using UnityEngine;

public class BoardVisualizer : MonoBehaviour
{
    [SerializeField] private Material _ourBoardMaterial;
    [SerializeField] private Material _enemyBoardMaterial;

    private void Start()
    {
        GenerateBoard();
    }

    private void GenerateBoard()
    {
        GridManager gm = GridManager.Instance;
        if (gm == null) return;

        CreateBoardPlane(gm, true, _ourBoardMaterial);
        CreateBoardPlane(gm, false, _enemyBoardMaterial);
        CreateGridLines(gm, true);
        CreateGridLines(gm, false);
    }

    private void CreateBoardPlane(GridManager gm, bool isOurSide, Material mat)
    {
        float w = gm.Width * gm.CellSize;
        float h = gm.Height * gm.CellSize;
        Vector3 center = gm.GetBoardCenter(isOurSide);

        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plane.name = isOurSide ? "OurBoardVisual" : "EnemyBoardVisual";
        plane.transform.position = center;
        plane.transform.localScale = new Vector3(w, 0.05f, h);

        Destroy(plane.GetComponent<Collider>());

        if (mat != null)
            plane.GetComponent<Renderer>().material = mat;
        else
            plane.GetComponent<Renderer>().material.color = isOurSide
                ? new Color(0.2f, 0.3f, 0.6f)
                : new Color(0.6f, 0.2f, 0.2f);
    }

    private void CreateGridLines(GridManager gm, bool isOurSide)
    {
        GameObject holder = new GameObject(isOurSide ? "OurGridLines" : "EnemyGridLines");
        float w = gm.Width * gm.CellSize;
        float h = gm.Height * gm.CellSize;
        Vector3 origin = gm.GetBoardOrigin(isOurSide);
        float lineY = 0.06f;
        Color color = isOurSide ? Color.cyan : Color.red;

        for (int x = 0; x <= gm.Width; x++)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = $"VLine_{x}";
            line.transform.parent = holder.transform;
            line.transform.position = origin + new Vector3(x * gm.CellSize, lineY, h / 2f);
            line.transform.localScale = new Vector3(0.04f, 0.02f, h);
            line.GetComponent<Renderer>().material.color = color;
            Destroy(line.GetComponent<Collider>());
        }
        for (int gy = 0; gy <= gm.Height; gy++)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = $"HLine_{gy}";
            line.transform.parent = holder.transform;
            line.transform.position = origin + new Vector3(w / 2f, lineY, gy * gm.CellSize);
            line.transform.localScale = new Vector3(w, 0.02f, 0.04f);
            line.GetComponent<Renderer>().material.color = color;
            Destroy(line.GetComponent<Collider>());
        }
    }
}
