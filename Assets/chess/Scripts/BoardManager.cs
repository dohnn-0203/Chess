using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("보드 설정")]
    [SerializeField] private int size = 8;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float tileHeight = 0.2f;

    [Header("참조")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private Transform boardRoot;

    [Header("타일 색")]
    [SerializeField] private Color whiteColor = Color.white;
    [SerializeField] private Color blackColor = new Color(0.16f, 0.16f, 0.16f);

    [Header("셰이더 색 이름")]
    [SerializeField] private string colorProp = "_BaseColor";

    private GameObject[,] tiles;
    private MaterialPropertyBlock block;

    private void Start()
    {
        block = new MaterialPropertyBlock();
        MakeBoard();
    }

    private void MakeBoard()
    {
        tiles = new GameObject[size, size];

        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                Vector3 pos = new Vector3(x * tileSize, 0f, z * tileSize);
                GameObject tileObj = Instantiate(tilePrefab, pos, Quaternion.identity, boardRoot);

                tileObj.name = $"Tile_{x}_{z}";
                tileObj.transform.localScale = new Vector3(tileSize, tileHeight, tileSize);

                SetTileColor(tileObj, x, z);
                SetTileData(tileObj, x, z);

                tiles[x, z] = tileObj;
            }
        }
    }

    // 타일 색 설정
    private void SetTileColor(GameObject tileObj, int x, int z)
    {
        Renderer rd = tileObj.GetComponent<Renderer>();
        if (rd == null) return;

        bool isWhite = (x + z) % 2 == 0;
        Color color = isWhite ? whiteColor : blackColor;

        block.Clear();
        block.SetColor(colorProp, color);
        rd.SetPropertyBlock(block);
    }

    // 타일 좌표 설정
    private void SetTileData(GameObject tileObj, int x, int z)
    {
        Tile tile = tileObj.GetComponent<Tile>();

        if (tile == null)
        {
            tile = tileObj.AddComponent<Tile>();
        }

        tile.Init(x, z);
    }
}