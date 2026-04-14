using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("보드 설정")]
    [SerializeField] private int size = 8;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float tileHeight = 0.2f;
    [SerializeField] private float pieceY = 0.6f;

    [Header("프리팹")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject pawnPrefab;

    [Header("부모 오브젝트")]
    [SerializeField] private Transform boardRoot;
    [SerializeField] private Transform piecesRoot;
    [SerializeField] private Transform whiteRoot;
    [SerializeField] private Transform blackRoot;

    [Header("타일 색")]
    [SerializeField] private Color whiteTileColor = Color.white;
    [SerializeField] private Color blackTileColor = new Color(0.16f, 0.16f, 0.16f);
    [SerializeField] private Color selectTileColor = new Color(0.8f, 0.8f, 0.2f);
    [SerializeField] private Color moveTileColor = new Color(0.4f, 0.8f, 0.4f);

    [Header("말 색")]
    [SerializeField] private Color whitePieceColor = new Color(0.9f, 0.9f, 0.9f);
    [SerializeField] private Color blackPieceColor = new Color(0.25f, 0.25f, 0.25f);

    [Header("셰이더 색 이름")]
    [SerializeField] private string colorKey = "_Color";

    private GameObject[,] tiles;
    private Piece[,] board;
    private MaterialPropertyBlock mpb;

    private Tile selectedTile;
    private Piece selectedPiece;
    private List<Vector2Int> moveList = new List<Vector2Int>();

    private Team turn = Team.White;

    private void Start()
    {
        mpb = new MaterialPropertyBlock();

        CreateBoard();
        CreatePawns();

        Debug.Log($"현재 턴: {turn}");
    }

    // 보드 생성
    private void CreateBoard()
    {
        tiles = new GameObject[size, size];
        board = new Piece[size, size];

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

    // 폰 생성
    private void CreatePawns()
    {
        for (int x = 0; x < size; x++)
        {
            SpawnPawn(Team.White, x, 1);
            SpawnPawn(Team.Black, x, 6);
        }
    }

    // 폰 하나 생성
    private void SpawnPawn(Team team, int x, int z)
    {
        Vector3 pos = GetPiecePos(x, z);
        Transform parent = team == Team.White ? whiteRoot : blackRoot;

        GameObject obj = Instantiate(pawnPrefab, pos, Quaternion.identity, parent);
        obj.name = $"{team}_Pawn_{x}_{z}";

        Pawn pawn = obj.GetComponent<Pawn>();
        if (pawn == null)
        {
            pawn = obj.AddComponent<Pawn>();
        }

        pawn.Init(team, PieceType.Pawn, x, z);
        SetPieceColor(obj, team);

        board[x, z] = pawn;
    }

    // 타일 클릭
    public void SelectTile(Tile tile)
    {
        Piece clickedPiece = board[tile.X, tile.Z];

        if (selectedPiece == null)
        {
            if (clickedPiece != null && clickedPiece.Team == turn)
            {
                SelectPiece(clickedPiece, tile);
            }

            return;
        }

        if (clickedPiece == selectedPiece)
        {
            ClearSelect();
            return;
        }

        Vector2Int target = new Vector2Int(tile.X, tile.Z);

        if (CanMove(target))
        {
            MovePiece(selectedPiece, tile.X, tile.Z);
            ChangeTurn();
            ClearSelect();
            return;
        }

        if (clickedPiece != null && clickedPiece.Team == turn)
        {
            ClearSelect();
            SelectPiece(clickedPiece, tile);
            return;
        }

        ClearSelect();
    }

    // 말 선택
    private void SelectPiece(Piece piece, Tile tile)
    {
        selectedPiece = piece;
        selectedTile = tile;

        SetObjectColor(tile.gameObject, selectTileColor);
        ShowMoves(piece);

        Debug.Log($"선택: {piece.Team} {piece.Type} ({piece.X}, {piece.Z})");
    }

    // 이동 칸 표시
    private void ShowMoves(Piece piece)
    {
        moveList.Clear();

        if (piece is Pawn pawn)
        {
            List<Vector2Int> moves = pawn.GetMoves(board, size);

            for (int i = 0; i < moves.Count; i++)
            {
                moveList.Add(moves[i]);
                SetObjectColor(tiles[moves[i].x, moves[i].y], moveTileColor);
            }
        }
    }

    // 이동 가능 여부
    private bool CanMove(Vector2Int pos)
    {
        for (int i = 0; i < moveList.Count; i++)
        {
            if (moveList[i] == pos)
            {
                return true;
            }
        }

        return false;
    }

    // 말 이동
    private void MovePiece(Piece piece, int x, int z)
    {
        Piece targetPiece = board[x, z];

        // 상대 말 제거
        if (targetPiece != null && targetPiece.Team != piece.Team)
        {
            Destroy(targetPiece.gameObject);
        }

        board[piece.X, piece.Z] = null;
        board[x, z] = piece;

        piece.transform.position = GetPiecePos(x, z);
        piece.SetPos(x, z);

        Debug.Log($"이동: ({x}, {z})");
    }

    // 턴 변경
    private void ChangeTurn()
    {
        turn = turn == Team.White ? Team.Black : Team.White;
        Debug.Log($"현재 턴: {turn}");
    }

    // 선택 해제
    private void ClearSelect()
    {
        if (selectedTile != null)
        {
            ResetTileColor(selectedTile.X, selectedTile.Z);
        }

        for (int i = 0; i < moveList.Count; i++)
        {
            ResetTileColor(moveList[i].x, moveList[i].y);
        }

        moveList.Clear();
        selectedTile = null;
        selectedPiece = null;
    }

    // 타일 색 설정
    private void SetTileColor(GameObject tileObj, int x, int z)
    {
        bool isWhite = (x + z) % 2 != 0;
        Color color = isWhite ? whiteTileColor : blackTileColor;
        SetObjectColor(tileObj, color);
    }

    // 타일 정보 저장
    private void SetTileData(GameObject tileObj, int x, int z)
    {
        Tile tile = tileObj.GetComponent<Tile>();

        if (tile == null)
        {
            tile = tileObj.AddComponent<Tile>();
        }

        tile.Init(x, z, this);
    }

    // 말 색 설정
    private void SetPieceColor(GameObject pieceObj, Team team)
    {
        Renderer[] rds = pieceObj.GetComponentsInChildren<Renderer>();
        if (rds == null || rds.Length == 0) return;

        Color color = team == Team.White ? whitePieceColor : blackPieceColor;

        for (int i = 0; i < rds.Length; i++)
        {
            mpb.Clear();
            mpb.SetColor(colorKey, color);
            rds[i].SetPropertyBlock(mpb);
        }
    }

    // 타일 원래 색 복구
    private void ResetTileColor(int x, int z)
    {
        SetTileColor(tiles[x, z], x, z);
    }

    // 단일 오브젝트 색 적용
    private void SetObjectColor(GameObject obj, Color color)
    {
        Renderer rd = obj.GetComponent<Renderer>();
        if (rd == null) return;

        mpb.Clear();
        mpb.SetColor(colorKey, color);
        rd.SetPropertyBlock(mpb);
    }

    // 말 위치 계산
    private Vector3 GetPiecePos(int x, int z)
    {
        return new Vector3(x * tileSize, pieceY, z * tileSize);
    }
    
}