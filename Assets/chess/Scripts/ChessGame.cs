using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

// 게임 진행
public class ChessGame : MonoBehaviour
{
    public TMP_Text logText;

    private BoardManager board;
    private ChessState state;
    private ChessRules rules;
    private ChessPromotion promotion;

    private Tile selectedTile;
    private Piece selectedPiece;
    private readonly List<ChessMove> moves = new List<ChessMove>();
    private readonly List<Vector2Int> moveTiles = new List<Vector2Int>();
    private readonly string[] logLines = new string[3];

    // 시작
    public void Init(BoardManager boardManager)
    {
        board = boardManager;
        state = new ChessState();
        rules = new ChessRules(board, state);
        promotion = new ChessPromotion(board, state, AddLog);

        AddLog($"Turn: {state.turn}");
    }

    // 매 프레임 체크
    private void Update()
    {
        if (!promotion.Update())
        {
            return;
        }

        EndTurn();
    }

    // 타일 클릭 처리
    public void ClickTile(Tile tile)
    {
        if (state.gameOver || state.waitPromotion)
        {
            return;
        }

        Piece clicked = board.GetPiece(tile.X, tile.Z);

        if (selectedPiece == null)
        {
            TrySelect(tile, clicked);
            return;
        }

        if (clicked == selectedPiece)
        {
            ClearSelect();
            return;
        }

        ChessMove move;
        if (TryGetMove(tile.X, tile.Z, out move))
        {
            rules.PlayMove(selectedPiece, move);
            promotion.Start(selectedPiece);
            ClearSelect();

            if (!state.waitPromotion)
            {
                EndTurn();
            }

            return;
        }

        if (clicked != null && clicked.Team == state.turn)
        {
            ClearSelect();
            TrySelect(tile, clicked);
            return;
        }

        ClearSelect();
    }

    // 말 선택
    private void TrySelect(Tile tile, Piece piece)
    {
        if (piece == null || piece.Team != state.turn)
        {
            return;
        }

        selectedTile = tile;
        selectedPiece = piece;

        board.HighlightSelect(tile.X, tile.Z);

        moves.Clear();
        moveTiles.Clear();

        List<ChessMove> list = rules.GetLegalMoves(piece);
        for (int i = 0; i < list.Count; i++)
        {
            moves.Add(list[i]);
            moveTiles.Add(list[i].to);
        }

        board.HighlightMoves(moveTiles);
    }

    // 선택 해제
    private void ClearSelect()
    {
        board.ClearHighlights(selectedTile, moveTiles);
        selectedTile = null;
        selectedPiece = null;
        moves.Clear();
        moveTiles.Clear();
    }

    // 이동 찾기
    private bool TryGetMove(int x, int z, out ChessMove move)
    {
        for (int i = 0; i < moves.Count; i++)
        {
            if (moves[i].to.x == x && moves[i].to.y == z)
            {
                move = moves[i];
                return true;
            }
        }

        move = default;
        return false;
    }

    // 턴 넘기기
    private void EndTurn()
    {
        state.turn = Other(state.turn);
        AddLog($"Turn: {state.turn}");

        bool check = rules.IsCheck(state.turn);
        bool hasMove = rules.HasMove(state.turn);

        if (!hasMove)
        {
            state.gameOver = true;

            if (check)
            {
                AddLog($"Checkmate! Winner: {Other(state.turn)}");
            }
            else
            {
                AddLog("Stalemate!");
            }

            return;
        }

        if (check)
        {
            AddLog($"{state.turn} is in check");
        }
    }

    // 로그 추가
    private void AddLog(string text)
    {
        Debug.Log(text);

        logLines[0] = logLines[1];
        logLines[1] = logLines[2];
        logLines[2] = text;

        RefreshLog();
    }

    // 로그 출력
    private void RefreshLog()
    {
        if (logText == null)
        {
            return;
        }

        string top = MakeLogLine(logLines[0], 0.25f);
        string mid = MakeLogLine(logLines[1], 0.55f);
        string bottom = MakeLogLine(logLines[2], 1f);

        logText.text = $"{top}\n{mid}\n{bottom}";
    }

    // 로그 한 줄 만들기
    private string MakeLogLine(string text, float alpha)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        string hex = Mathf.RoundToInt(alpha * 255f).ToString("X2", CultureInfo.InvariantCulture);
        return $"<alpha=#{hex}>{text}";
    }

    // 상대 팀
    private Team Other(Team team)
    {
        return team == Team.White ? Team.Black : Team.White;
    }
}
