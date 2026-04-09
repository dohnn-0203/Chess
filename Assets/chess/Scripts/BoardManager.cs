using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("보드 설정")]
    [SerializeField] private int size = 8;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private float tileHeight = 0.2f;
    [SerializeField] private float pieceY = 0.6f;

    [Header("참조")]
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private GameObject piecePrefab;
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
    [SerializeField] private string colorProp = "_BaseColor";

    private GameObject[,] tiles;
    private Piece[,] pieces;
    private MaterialPropertyBlock block;

    private Tile selectedTile;
    private Piece selectedPiece;
    private List<Vector2Int> moveTiles = new List<Vector2Int>();

    private void Start()
    {
        block = new MaterialPropertyBlock();

        MakeBoard();
        MakePawns();
    }

    // 보드 생성
    private void MakeBoard()
    {
        tiles = new GameObject[size, size];
        pieces = new Piece[size, size];

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

    // 폰 배치
    private void MakePawns()
    {
        for (int x = 0; x < size; x++)
        {
            SpawnPawn(Team.White, x, 1);
            SpawnPawn(Team.Black, x, 6);
        }
    }

    // 폰 생성
    private void SpawnPawn(Team team, int x, int z)
    {
        Vector3 pos = GetPiecePos(x, z);
        Transform parent = team == Team.White ? whiteRoot : blackRoot;

        GameObject obj = Instantiate(piecePrefab, pos, Quaternion.identity, parent);
        obj.name = $"{team}_Pawn_{x}_{z}";

        Pawn pawn = obj.GetComponent<Pawn>();
        if (pawn == null)
        {
            pawn = obj.AddComponent<Pawn>();
        }

        pawn.Init(team, PieceType.Pawn, x, z);
        SetPieceColor(obj, team);

        pieces[x, z] = pawn;
    }

    // 타일 클릭
    public void SelectTile(Tile tile)
    {
        Piece clickedPiece = pieces[tile.X, tile.Z];

        if (selectedPiece == null)
        {
            if (clickedPiece != null)
            {
                SelectPiece(clickedPiece, tile);
            }

            return;
        }

        if (clickedPiece == selectedPiece)
        {
            ClearSelection();
            return;
        }

        Vector2Int pos = new Vector2Int(tile.X, tile.Z);

        if (CanMove(pos))
        {
            MovePiece(selectedPiece, tile.X, tile.Z);
            ClearSelection();
            return;
        }

        if (clickedPiece != null)
        {
            ClearSelection();
            SelectPiece(clickedPiece, tile);
            return;
        }

        ClearSelection();
    }

    // 말 선택
    private void SelectPiece(Piece piece, Tile tile)
    {
        selectedPiece = piece;
        selectedTile = tile;

        SetColor(tile.gameObject, selectTileColor);
        ShowMoves(piece);

        Debug.Log($"선택한 말: {piece.Team} {piece.Type} ({piece.X}, {piece.Z})");
    }

    // 이동 가능 칸 표시
    private void ShowMoves(Piece piece)
    {
        moveTiles.Clear();

        if (piece is Pawn pawn)
        {
            List<Vector2Int> moves = pawn.GetMoves(pieces, size);

            for (int i = 0; i < moves.Count; i++)
            {
                moveTiles.Add(moves[i]);
                SetColor(tiles[moves[i].x, moves[i].y], moveTileColor);
            }
        }
    }

    // 이동 가능 여부
    private bool CanMove(Vector2Int pos)
    {
        for (int i = 0; i < moveTiles.Count; i++)
        {
            if (moveTiles[i] == pos)
            {
                return true;
            }
        }

        return false;
    }

    // 말 이동
    private void MovePiece(Piece piece, int newX, int newZ)
    {
        pieces[piece.X, piece.Z] = null;
        pieces[newX, newZ] = piece;

        piece.transform.position = GetPiecePos(newX, newZ);
        piece.SetPos(newX, newZ);

        Debug.Log($"이동 완료: ({newX}, {newZ})");
    }

    // 선택 해제
    private void ClearSelection()
    {
        if (selectedTile != null)
        {
            ResetTileColor(selectedTile.X, selectedTile.Z);
        }

        for (int i = 0; i < moveTiles.Count; i++)
        {
            ResetTileColor(moveTiles[i].x, moveTiles[i].y);
        }

        moveTiles.Clear();
        selectedTile = null;
        selectedPiece = null;
    }

    // 타일 기본 색
    private void SetTileColor(GameObject tileObj, int x, int z)
    {
        bool isWhite = (x + z) % 2 != 0; // (0, 0) 왼쪽 아래 첫 칸이 흰색
        Color color = isWhite ? whiteTileColor : blackTileColor;
        SetColor(tileObj, color);
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
        Renderer[] renderers = pieceObj.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0) return;

        Color color = team == Team.White ? whitePieceColor : blackPieceColor;

        for (int i = 0; i < renderers.Length; i++)
        {
            block.Clear();
            block.SetColor(colorProp, color);
            renderers[i].SetPropertyBlock(block);
        }
    }

    // 타일 색 복구
    private void ResetTileColor(int x, int z)
    {
        SetTileColor(tiles[x, z], x, z);
    }

    // 단일 오브젝트 색 적용
    private void SetColor(GameObject obj, Color color)
    {
        Renderer rd = obj.GetComponent<Renderer>();
        if (rd == null) return;

        block.Clear();
        block.SetColor(colorProp, color);
        rd.SetPropertyBlock(block);
    }

    // 말 위치 계산
    private Vector3 GetPiecePos(int x, int z)
    {
        return new Vector3(x * tileSize, pieceY, z * tileSize);
    }
}