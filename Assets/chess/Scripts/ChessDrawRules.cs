using System.Collections.Generic;
using System.Text;

// 무승부 조건과 동일한 포지션의 반복 횟수 관리
public class ChessDrawRules
{
    private readonly BoardManager board;
    private readonly ChessState state;
    private readonly ChessRules rules;
    private readonly Dictionary<string, int> positions = new Dictionary<string, int>();

    public int RepetitionCount { get; private set; }

    public bool CanClaimDraw
    {
        get { return !state.gameOver && !state.waitPromotion && ClaimReason.Length > 0; }
    }

    public string ClaimReason
    {
        get
        {
            if (RepetitionCount >= 3)
            {
                return "Draw by threefold repetition";
            }

            return state.halfmoveClock >= 100 ? "Draw by 50-move rule" : string.Empty;
        }
    }

    // 체크메이트 판정 후 확인해야 마지막 수의 체크메이트가 우선한다.
    public string AutomaticDrawReason
    {
        get
        {
            if (IsInsufficientMaterial())
            {
                return "Draw by insufficient material";
            }

            if (RepetitionCount >= 5)
            {
                return "Draw by fivefold repetition";
            }

            return state.halfmoveClock >= 150 ? "Draw by 75-move rule" : string.Empty;
        }
    }

    public ChessDrawRules(BoardManager boardManager, ChessState gameState, ChessRules chessRules)
    {
        board = boardManager;
        state = gameState;
        rules = chessRules;
    }

    // 시작 배치와 승급까지 끝난 각 턴의 배치를 한 번씩 기록한다.
    public void RecordPosition()
    {
        string key = PositionKey();
        int count;
        positions.TryGetValue(key, out count);
        RepetitionCount = count + 1;
        positions[key] = RepetitionCount;
    }

    public bool IsInsufficientMaterial()
    {
        int knights = 0;
        int bishops = 0;
        int bishopColor = -1;
        bool sameColorBishops = true;

        for (int x = 0; x < board.Size; x++)
        {
            for (int z = 0; z < board.Size; z++)
            {
                Piece piece = board.GetPiece(x, z);
                if (piece == null || piece.Type == PieceType.King)
                {
                    continue;
                }

                if (piece.Type == PieceType.Knight)
                {
                    knights++;
                }
                else if (piece.Type == PieceType.Bishop)
                {
                    bishops++;
                    int color = (x + z) % 2;
                    if (bishopColor >= 0 && bishopColor != color)
                    {
                        sameColorBishops = false;
                    }

                    bishopColor = color;
                }
                else
                {
                    return false;
                }
            }
        }

        // 두 나이트 또는 서로 다른 색의 비숍은 자동 무승부로 처리하지 않는다.
        return (bishops == 0 && knights <= 1)
            || (knights == 0 && sameColorBishops);
    }

    private string PositionKey()
    {
        StringBuilder key = new StringBuilder(80);
        key.Append(state.turn == Team.White ? 'w' : 'b');

        for (int z = 0; z < board.Size; z++)
        {
            for (int x = 0; x < board.Size; x++)
            {
                Piece piece = board.GetPiece(x, z);
                if (piece == null)
                {
                    key.Append('.');
                    continue;
                }

                // PieceType 값과 팀을 하나의 고유한 문자로 표현한다.
                int code = (int)piece.Type + (piece.Team == Team.White ? 0 : 6);
                key.Append((char)('A' + code));
            }
        }

        AppendCastleRight(key, Team.White, true, 'K');
        AppendCastleRight(key, Team.White, false, 'Q');
        AppendCastleRight(key, Team.Black, true, 'k');
        AppendCastleRight(key, Team.Black, false, 'q');
        key.Append('|');

        // 실제로 둘 수 없는 앙파상은 동일 포지션 판정에 영향을 주지 않는다.
        if (HasLegalEnPassant())
        {
            key.Append(state.enPassantTile.x);
            key.Append(',');
            key.Append(state.enPassantTile.y);
        }

        return key.ToString();
    }

    private void AppendCastleRight(StringBuilder key, Team team, bool kingSide, char symbol)
    {
        int rank = team == Team.White ? 0 : board.Size - 1;
        Piece king = board.GetPiece(4, rank);
        Piece rook = board.GetPiece(kingSide ? board.Size - 1 : 0, rank);
        if (king != null && king.Team == team && king.Type == PieceType.King && !king.HasMoved
            && rook != null && rook.Team == team && rook.Type == PieceType.Rook && !rook.HasMoved)
        {
            key.Append(symbol);
        }
    }

    private bool HasLegalEnPassant()
    {
        if (!state.canEnPassant)
        {
            return false;
        }

        int rank = state.enPassantPawn.y;
        for (int offset = -1; offset <= 1; offset += 2)
        {
            int file = state.enPassantPawn.x + offset;
            if (!board.IsInside(file, rank))
            {
                continue;
            }

            Piece piece = board.GetPiece(file, rank);
            if (piece == null || piece.Team != state.turn || piece.Type != PieceType.Pawn)
            {
                continue;
            }

            List<ChessMove> moves = rules.GetLegalMoves(piece);
            for (int i = 0; i < moves.Count; i++)
            {
                if (moves[i].enPassant)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
