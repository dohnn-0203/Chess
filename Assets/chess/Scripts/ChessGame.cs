using System.Collections.Generic;
using System.Globalization;
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
    private readonly string[] logLines = new string[3];
    private string result = string.Empty;
    private string pendingMove;
    private Vector2Int lastFrom;
    private Vector2Int lastTo;
    private bool hasLastMove;
    private bool inCheck;
    private bool flipped;
    private bool cameraPoseSaved;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
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
    public string RecentLogText => MakeLogLine(logLines[0], 0.25f) + "\n"
        + MakeLogLine(logLines[1], 0.55f) + "\n" + MakeLogLine(logLines[2], 1f);

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
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = false;
#endif
        board = boardManager;
        if (logText != null)
        {
            logText.gameObject.SetActive(true);
            logText.raycastTarget = false;
        }
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
        AddLog("Turn: " + Turn);
        if (inCheck && hasMove) AddLog(Turn + " is in check");
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
        promotion = new ChessPromotion(board, state, AddLog);
        draws = new ChessDrawRules(board, state, rules);
        draws.RecordPosition();
        history.Clear();
        whiteCaptured.Clear();
        blackCaptured.Clear();
        result = string.Empty;
        pendingMove = null;
        hasLastMove = false;
        inCheck = false;
        System.Array.Clear(logLines, 0, logLines.Length);
        AddLog("Turn: " + Turn);
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
        AddLog(text);
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
        board.OrientCoordinates(flipped);
        ConfigureCamera();
    }

    private void ConfigureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null) return;
        screenWidth = Screen.width;
        screenHeight = Screen.height;
        if (!cameraPoseSaved)
        {
            originalCameraPosition = camera.transform.position;
            originalCameraRotation = camera.transform.rotation;
            cameraPoseSaved = true;
        }
        Vector3 center = new Vector3(3.5f * board.TileSize, 0f, 3.5f * board.TileSize);
        // 원본 원근과 각도를 유지하며 작은 임베드 화면에서도 모든 기물을 보이게 한다.
        float tangent = Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 0.90f;
        float distance = 0f;
        Quaternion inverse = Quaternion.Inverse(originalCameraRotation);
        for (int x = 0; x < 2; x++)
            for (int y = 0; y < 2; y++)
                for (int z = 0; z < 2; z++)
                {
                    Vector3 corner = new Vector3(x == 0 ? -0.6f : 7.6f, y == 0 ? 0f : 1.5f, z == 0 ? -0.6f : 7.6f) * board.TileSize;
                    Vector3 local = inverse * (corner - originalCameraPosition);
                    distance = Mathf.Max(distance, Mathf.Abs(local.y) / tangent - local.z,
                        Mathf.Abs(local.x) / (tangent * Mathf.Max(0.3f, camera.aspect)) - local.z);
                }
        Vector3 position = originalCameraPosition - originalCameraRotation * Vector3.forward * distance;
        Quaternion rotation = flipped ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
        camera.transform.position = center + rotation * (position - center);
        camera.transform.rotation = rotation * originalCameraRotation;
    }

    private void AddLog(string text)
    {
        logLines[0] = logLines[1];
        logLines[1] = logLines[2];
        logLines[2] = text;
        if (logText != null) logText.text = RecentLogText;
        Debug.Log(text);
    }

    private static string MakeLogLine(string text, float alpha)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        string hex = Mathf.RoundToInt(alpha * 255f).ToString("X2", CultureInfo.InvariantCulture);
        return "<alpha=#" + hex + ">" + text;
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
