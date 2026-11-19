using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

// 게임 진행과 사용자 명령
public class ChessGame : MonoBehaviour
{
    public TMP_Text logText;

    private BoardManager board;
    private ChessState state;
    private ChessRules rules;
    private ChessPromotion promotion;
    private ChessDrawRules draws;
    private ChessUI ui;
    private Piece selectedPiece;
    private readonly List<ChessMove> moves = new List<ChessMove>();
    private readonly List<Vector2Int> moveTiles = new List<Vector2Int>();
    private readonly List<string> history = new List<string>();
    private readonly List<PieceType> whiteCaptured = new List<PieceType>();
    private readonly List<PieceType> blackCaptured = new List<PieceType>();
    private string result = string.Empty;
    private string pendingMove;
    private Vector2Int lastFrom;
    private Vector2Int lastTo;
    private bool hasLastMove;
    private bool inCheck;
    private bool flipped;
    private int screenWidth;
    private int screenHeight;

    public Team Turn => state != null ? state.turn : Team.White;
    public bool IsGameOver => state != null && state.gameOver;
    public bool IsPromotionPending => state != null && state.waitPromotion;
    public bool CanClaimDraw => draws != null && draws.CanClaimDraw;
    public bool HasMoves => history.Count > 0 || IsPromotionPending;
    public bool CanInteract => state != null && !IsGameOver && !IsPromotionPending && (ui == null || !ui.IsBlockingInput);
    public string StatusText => IsGameOver ? result : IsPromotionPending ? Turn + " - choose promotion" : Turn + " to move" + (inCheck ? " - CHECK" : "");
    public string WhiteCapturedText => CapturedText(whiteCaptured);
    public string BlackCapturedText => CapturedText(blackCaptured);

    public string HistoryText
    {
        get
        {
            StringBuilder text = new StringBuilder();
            for (int i = 0; i < history.Count; i += 2)
            {
                if (i > 0) text.Append('\n');
                text.Append(i / 2 + 1).Append(".  ").Append(history[i]);
                if (i + 1 < history.Count) text.Append("    ").Append(history[i + 1]);
            }
            return text.ToString();
        }
    }

    public void Init(BoardManager boardManager)
    {
        board = boardManager;
        if (logText != null) logText.gameObject.SetActive(false);
        ui = GetComponent<ChessUI>();
        if (ui == null) ui = gameObject.AddComponent<ChessUI>();
        ResetState();
        ui.Init(this);
        ConfigureCamera();
        ui.Refresh();
    }

    private void Update()
    {
        if (state == null) return;
        if (screenWidth != Screen.width || screenHeight != Screen.height) ConfigureCamera();
        if (ui != null && ui.IsBlockingInput) return;
        if (Input.GetKeyDown(KeyCode.Escape)) ClearSelect();
        if (Input.GetKeyDown(KeyCode.F)) FlipBoard();
        if (promotion.Update()) CompletePromotion();
    }

    public void ClickTile(Tile tile)
    {
        if (!CanInteract || tile == null || !board.IsInside(tile.X, tile.Z)) return;
        Piece clicked = board.GetPiece(tile.X, tile.Z);
        if (clicked == selectedPiece && selectedPiece != null)
        {
            ClearSelect();
            return;
        }
        if (selectedPiece != null)
        {
            for (int i = 0; i < moves.Count; i++)
            {
                if (moves[i].to.x != tile.X || moves[i].to.y != tile.Z) continue;
                PlayMove(moves[i]);
                return;
            }
        }
        ClearSelect();
        if (clicked == null || clicked.Team != Turn) return;
        selectedPiece = clicked;
        moves.AddRange(rules.GetLegalMoves(clicked));
        foreach (ChessMove move in moves) moveTiles.Add(move.to);
        board.HighlightSelect(tile.X, tile.Z);
        board.HighlightMoves(moveTiles);
    }

    private void PlayMove(ChessMove move)
    {
        Piece moving = selectedPiece;
        Piece captured = move.enPassant ? board.GetPiece(move.enPassantPos.x, move.enPassantPos.y) : board.GetPiece(move.to.x, move.to.y);
        lastFrom = new Vector2Int(moving.X, moving.Z);
        lastTo = move.to;
        hasLastMove = true;
        pendingMove = move.castle ? (move.to.x > moving.X ? "O-O" : "O-O-O")
            : PieceLetter(moving.Type) + Square(lastFrom) + (captured != null ? "x" : "-") + Square(lastTo) + (move.enPassant ? " e.p." : "");
        if (captured != null) (Turn == Team.White ? whiteCaptured : blackCaptured).Add(captured.Type);
        rules.PlayMove(moving, move);
        promotion.Start(moving);
        ClearSelect();
        if (!IsPromotionPending) EndTurn();
        else ui.Refresh();
    }

    public void Promote(PieceType type)
    {
        if (promotion != null && (ui == null || !ui.IsBlockingInput) && promotion.Promote(type)) CompletePromotion();
    }

    private void CompletePromotion()
    {
        Piece piece = board.GetPiece(lastTo.x, lastTo.y);
        pendingMove += "=" + PieceLetter(piece.Type);
        EndTurn();
    }

    private void EndTurn()
    {
        state.turn = Other(Turn);
        inCheck = rules.IsCheck(Turn);
        bool hasMove = rules.HasMove(Turn);
        if (inCheck) pendingMove += hasMove ? "+" : "#";
        history.Add(pendingMove);
        pendingMove = null;
        draws.RecordPosition();
        if (!hasMove) Finish(inCheck ? "Checkmate - " + Other(Turn) + " wins" : "Draw by stalemate");
        else if (draws.AutomaticDrawReason.Length > 0) Finish(draws.AutomaticDrawReason);
        RepaintBoard();
        ui.Refresh();
    }

    public void RestartGame()
    {
        if (board == null) return;
        ClearSelect();
        board.ResetPieces();
        ResetState();
        RepaintBoard();
        ui.Refresh();
    }

    private void ResetState()
    {
        state = new ChessState();
        rules = new ChessRules(board, state);
        promotion = new ChessPromotion(board, state, Debug.Log);
        draws = new ChessDrawRules(board, state, rules);
        draws.RecordPosition();
        history.Clear();
        whiteCaptured.Clear();
        blackCaptured.Clear();
        result = string.Empty;
        pendingMove = null;
        hasLastMove = false;
        inCheck = false;
    }

    public void Resign()
    {
        if (state == null || IsGameOver || IsPromotionPending) return;
        Finish(Turn + " resigned - " + Other(Turn) + " wins");
        ui.Refresh();
    }

    public void ClaimDraw()
    {
        if (!CanClaimDraw) return;
        Finish(draws.ClaimReason);
        ui.Refresh();
    }

    private void Finish(string text)
    {
        state.gameOver = true;
        result = text;
        ClearSelect();
        Debug.Log(text);
    }

    private void ClearSelect()
    {
        selectedPiece = null;
        moves.Clear();
        moveTiles.Clear();
        RepaintBoard();
    }

    private void RepaintBoard()
    {
        board.ResetHighlights();
        if (hasLastMove) board.HighlightLastMove(lastFrom, lastTo);
        if (inCheck) board.HighlightCheck(Turn);
    }

    public void FlipBoard()
    {
        flipped = !flipped;
        ConfigureCamera();
    }

    private void ConfigureCamera()
    {
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        Camera camera = Camera.main;
        if (camera == null) return;
        camera.rect = new Rect(0f, 0f, ChessUI.BoardViewportWidth, 1f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.055f, 0.075f, 0.10f);
        camera.orthographic = true;
        float tile = board.TileSize;
        camera.orthographicSize = Mathf.Max(5.4f, 4.9f / Mathf.Max(0.3f, camera.aspect)) * tile;
        Vector3 center = new Vector3(3.5f * tile, 0.25f, 3.5f * tile);
        camera.transform.position = center + new Vector3(0f, 11f, flipped ? 8f : -8f) * tile;
        camera.transform.LookAt(center);
    }

    private static string CapturedText(List<PieceType> captured)
    {
        if (captured.Count == 0) return "None";
        StringBuilder text = new StringBuilder();
        foreach (PieceType type in captured) text.Append(type == PieceType.Pawn ? "P" : PieceLetter(type)).Append(' ');
        return text.ToString().TrimEnd();
    }

    private static string Square(Vector2Int square) => ((char)('a' + square.x)).ToString() + (square.y + 1);
    private static Team Other(Team team) => team == Team.White ? Team.Black : Team.White;
    private static string PieceLetter(PieceType type)
    {
        switch (type)
        {
            case PieceType.King: return "K";
            case PieceType.Queen: return "Q";
            case PieceType.Rook: return "R";
            case PieceType.Bishop: return "B";
            case PieceType.Knight: return "N";
            default: return string.Empty;
        }
    }
}
