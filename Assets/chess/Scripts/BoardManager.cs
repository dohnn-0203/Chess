using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// 보드 관리
public class BoardManager : MonoBehaviour
{
    [Header("Board")]
    [SerializeField] private int size = 8;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float tileHeight = 0.2f;
    [SerializeField] private float pieceY = 0.6f;

    [Header("Prefabs")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject pawnPrefab;
    [SerializeField] private GameObject rookPrefab;
    [SerializeField] private GameObject knightPrefab;
    [SerializeField] private GameObject bishopPrefab;
    [SerializeField] private GameObject queenPrefab;
    [SerializeField] private GameObject kingPrefab;

    [Header("Roots")]
    [SerializeField] private Transform boardRoot;
    [SerializeField] private Transform piecesRoot;
    [SerializeField] private Transform whiteRoot;
    [SerializeField] private Transform blackRoot;

    [Header("Tile Colors")]
    [SerializeField] private Color whiteTileColor = Color.white;
    [SerializeField] private Color blackTileColor = new Color(0.16f, 0.16f, 0.16f);
    [SerializeField] private Color selectColor = new Color(0.8f, 0.8f, 0.2f);
    [SerializeField] private Color moveColor = new Color(0.4f, 0.8f, 0.4f);

    [Header("Piece Colors")]
    [SerializeField] private Color whitePieceColor = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private Color blackPieceColor = new Color(0.25f, 0.25f, 0.25f);

    [Header("Shader")]
    [SerializeField] private string colorKey = "_Color";

    private GameObject[,] tiles;
    private Piece[,] pieces;
    private MaterialPropertyBlock block;
    private ChessGame game;

    public int Size => size;
    public float TileSize => tileSize;

    // 시작
    private void Start()
    {
        size = 8;
        block = new MaterialPropertyBlock();
        tiles = new GameObject[size, size];
        pieces = new Piece[size, size];

        CreateBoard();
        CreatePieces();

        game = GetComponent<ChessGame>();
        if (game != null)
        {
            game.Init(this);
        }
    }

    // UI 입력과 보드 입력을 한 곳에서 구분
    private void Update()
    {
        if (game == null || !game.CanInteract || !Input.GetMouseButtonDown(0)) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        Camera camera = Camera.main;
        if (camera == null || !camera.pixelRect.Contains(Input.mousePosition)) return;
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000f))
        {
            Piece piece = hit.collider.GetComponentInParent<Piece>();
            if (piece != null)
            {
                ClickTile(tiles[piece.X, piece.Z].GetComponent<Tile>());
                return;
            }
            Tile tile = hit.collider.GetComponent<Tile>();
            if (tile != null)
            {
                ClickTile(tile);
                return;
            }
        }
    }

    public void ResetPieces()
    {
        for (int x = 0; x < size; x++)
            for (int z = 0; z < size; z++)
                RemovePiece(pieces[x, z]);
        CreatePieces();
        ResetHighlights();
    }

    public void ResetHighlights()
    {
        for (int x = 0; x < size; x++)
            for (int z = 0; z < size; z++)
                ResetTile(x, z);
    }

    public void HighlightLastMove(Vector2Int from, Vector2Int to)
    {
        Color lastColor = new Color(0.52f, 0.58f, 0.32f);
        SetTileColor(tiles[from.x, from.y], lastColor);
        SetTileColor(tiles[to.x, to.y], lastColor);
    }

    public void HighlightCheck(Team team)
    {
        for (int x = 0; x < size; x++)
            for (int z = 0; z < size; z++)
            {
                Piece piece = pieces[x, z];
                if (piece != null && piece.Team == team && piece.Type == PieceType.King)
                    SetTileColor(tiles[x, z], new Color(0.85f, 0.25f, 0.22f));
            }
    }



    // 타일 클릭
    public void ClickTile(Tile tile)
    {
        if (game == null)
        {
            return;
        }

        game.ClickTile(tile);
    }

    // 범위 체크
    public bool IsInside(int x, int z)
    {
        return x >= 0 && x < size && z >= 0 && z < size;
    }

    // 말 가져오기
    public Piece GetPiece(int x, int z)
    {
        return pieces[x, z];
    }

    // 말 넣기
    public void SetPiece(int x, int z, Piece piece)
    {
        pieces[x, z] = piece;
    }

    // 말 이동
    public void MovePiece(Piece piece, int x, int z)
    {
        piece.transform.position = GetPiecePos(x, z);
        piece.SetPos(x, z);
        pieces[x, z] = piece;
    }

    // 말 삭제
    public void RemovePiece(Piece piece)
    {
        if (piece == null)
        {
            return;
        }

        if (pieces[piece.X, piece.Z] == piece) pieces[piece.X, piece.Z] = null;
        piece.gameObject.SetActive(false);
        Destroy(piece.gameObject);
    }

    // 선택 칸 표시
    public void HighlightSelect(int x, int z)
    {
        SetTileColor(tiles[x, z], selectColor);
    }

    // 이동 칸 표시
    public void HighlightMoves(List<Vector2Int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            Vector2Int pos = list[i];
            SetTileColor(tiles[pos.x, pos.y], moveColor);
        }
    }

    // 표시 지우기
    public void ClearHighlights(Tile tile, List<Vector2Int> list)
    {
        if (tile != null)
        {
            ResetTile(tile.X, tile.Z);
        }

        for (int i = 0; i < list.Count; i++)
        {
            Vector2Int pos = list[i];
            ResetTile(pos.x, pos.y);
        }
    }

    // 말 생성
    public Piece SpawnPiece(PieceType type, Team team, int x, int z)
    {
        GameObject prefab = GetPiecePrefab(type);
        if (prefab == null)
        {
            return null;
        }

        GameObject obj = Instantiate(prefab, GetPiecePos(x, z), Quaternion.identity, GetRoot(team));
        obj.name = $"{team}_{type}_{x}_{z}";

        Piece piece = AddPieceScript(obj, type);
        piece.Init(team, type, x, z);
        SetPieceColor(obj, team);
        if (obj.GetComponentInChildren<Collider>() == null)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                BoxCollider collider = obj.AddComponent<BoxCollider>();
                collider.center = obj.transform.InverseTransformPoint(bounds.center);
                Vector3 scale = obj.transform.lossyScale;
                collider.size = new Vector3(bounds.size.x / Mathf.Abs(scale.x), bounds.size.y / Mathf.Abs(scale.y), bounds.size.z / Mathf.Abs(scale.z));
            }
        }
        pieces[x, z] = piece;

        return piece;
    }



    // 보드 생성
    private void CreateBoard()
    {
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                Vector3 pos = new Vector3(x * tileSize, 0f, z * tileSize);
                GameObject obj = Instantiate(tilePrefab, pos, Quaternion.identity, boardRoot);

                obj.name = $"Tile_{x}_{z}";
                obj.transform.localScale = new Vector3(tileSize, tileHeight, tileSize);

                Tile tile = obj.GetComponent<Tile>();
                if (tile == null)
                {
                    tile = obj.AddComponent<Tile>();
                }

                tile.Init(x, z, this);
                ResetTile(x, z, obj);
                tiles[x, z] = obj;
            }
        }
        CreateCoordinates();
    }

    private void CreateCoordinates()
    {
        for (int i = 0; i < size; i++)
        {
            CreateLabel(((char)('a' + i)).ToString(), new Vector3(i * tileSize, 0.12f, -0.8f * tileSize));
            CreateLabel(((char)('a' + i)).ToString(), new Vector3(i * tileSize, 0.12f, 7.8f * tileSize));
            CreateLabel((i + 1).ToString(), new Vector3(-0.8f * tileSize, 0.12f, i * tileSize));
            CreateLabel((i + 1).ToString(), new Vector3(7.8f * tileSize, 0.12f, i * tileSize));
        }
    }

    private void CreateLabel(string value, Vector3 position)
    {
        GameObject obj = new GameObject("Coordinate_" + value);
        obj.transform.SetParent(boardRoot, false);
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        TextMesh label = obj.AddComponent<TextMesh>();
        label.text = value;
        label.fontSize = 64;
        label.characterSize = 0.065f * tileSize;
        label.anchor = TextAnchor.MiddleCenter;
        label.color = new Color(0.88f, 0.83f, 0.67f);
    }

    // 시작 배치
    private void CreatePieces()
    {
        for (int x = 0; x < size; x++)
        {
            SpawnPiece(PieceType.Pawn, Team.White, x, 1);
            SpawnPiece(PieceType.Pawn, Team.Black, x, 6);
        }

        SpawnPiece(PieceType.Rook, Team.White, 0, 0);
        SpawnPiece(PieceType.Rook, Team.White, 7, 0);
        SpawnPiece(PieceType.Rook, Team.Black, 0, 7);
        SpawnPiece(PieceType.Rook, Team.Black, 7, 7);

        SpawnPiece(PieceType.Knight, Team.White, 1, 0);
        SpawnPiece(PieceType.Knight, Team.White, 6, 0);
        SpawnPiece(PieceType.Knight, Team.Black, 1, 7);
        SpawnPiece(PieceType.Knight, Team.Black, 6, 7);

        SpawnPiece(PieceType.Bishop, Team.White, 2, 0);
        SpawnPiece(PieceType.Bishop, Team.White, 5, 0);
        SpawnPiece(PieceType.Bishop, Team.Black, 2, 7);
        SpawnPiece(PieceType.Bishop, Team.Black, 5, 7);

        SpawnPiece(PieceType.Queen, Team.White, 3, 0);
        SpawnPiece(PieceType.Queen, Team.Black, 3, 7);

        SpawnPiece(PieceType.King, Team.White, 4, 0);
        SpawnPiece(PieceType.King, Team.Black, 4, 7);
    }

    // 프리팹 찾기
    private GameObject GetPiecePrefab(PieceType type)
    {
        switch (type)
        {
            case PieceType.Pawn:
                return pawnPrefab;
            case PieceType.Rook:
                return rookPrefab;
            case PieceType.Knight:
                return knightPrefab;
            case PieceType.Bishop:
                return bishopPrefab;
            case PieceType.Queen:
                return queenPrefab;
            case PieceType.King:
                return kingPrefab;
            default:
                return null;
        }
    }

    // 컴포넌트 붙이기
    private Piece AddPieceScript(GameObject obj, PieceType type)
    {
        switch (type)
        {
            case PieceType.Pawn:
            {
                Pawn pawn = obj.GetComponent<Pawn>();
                return pawn != null ? pawn : obj.AddComponent<Pawn>();
            }
            case PieceType.Rook:
            {
                Rook rook = obj.GetComponent<Rook>();
                return rook != null ? rook : obj.AddComponent<Rook>();
            }
            case PieceType.Knight:
            {
                Knight knight = obj.GetComponent<Knight>();
                return knight != null ? knight : obj.AddComponent<Knight>();
            }
            case PieceType.Bishop:
            {
                Bishop bishop = obj.GetComponent<Bishop>();
                return bishop != null ? bishop : obj.AddComponent<Bishop>();
            }
            case PieceType.Queen:
            {
                Queen queen = obj.GetComponent<Queen>();
                return queen != null ? queen : obj.AddComponent<Queen>();
            }
            default:
            {
                King king = obj.GetComponent<King>();
                return king != null ? king : obj.AddComponent<King>();
            }
        }
    }

    // 부모 찾기
    private Transform GetRoot(Team team)
    {
        if (team == Team.White)
        {
            return whiteRoot != null ? whiteRoot : piecesRoot;
        }

        return blackRoot != null ? blackRoot : piecesRoot;
    }

    // 타일 초기화
    private void ResetTile(int x, int z)
    {
        ResetTile(x, z, tiles[x, z]);
    }

    // 타일 색 원복
    private void ResetTile(int x, int z, GameObject obj)
    {
        bool isWhite = (x + z) % 2 != 0;
        SetTileColor(obj, isWhite ? whiteTileColor : blackTileColor);
    }

    // 타일 색 변경
    private void SetTileColor(GameObject obj, Color color)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        block.Clear();
        block.SetColor(colorKey, color);
        renderer.SetPropertyBlock(block);
    }

    // 말 색 변경
    private void SetPieceColor(GameObject obj, Team team)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0)
        {
            return;
        }

        Color color = team == Team.White ? whitePieceColor : blackPieceColor;

        for (int i = 0; i < renderers.Length; i++)
        {
            block.Clear();
            block.SetColor(colorKey, color);
            renderers[i].SetPropertyBlock(block);
        }
    }

    // 좌표 변환
    private Vector3 GetPiecePos(int x, int z)
    {
        return new Vector3(x * tileSize, pieceY, z * tileSize);
    }
}
