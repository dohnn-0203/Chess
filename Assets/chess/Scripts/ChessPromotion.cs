using UnityEngine;

// 폰 승급
public class ChessPromotion
{
    private readonly BoardManager board;
    private readonly ChessState state;
    private readonly System.Action<string> log;

    // 승급 준비
    public ChessPromotion(BoardManager boardManager, ChessState gameState, System.Action<string> logAction)
    {
        board = boardManager;
        state = gameState;
        log = logAction;
    }

    // 입력 체크
    public bool Update()
    {
        if (!state.waitPromotion)
        {
            return false;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            return Promote(PieceType.Queen);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            return Promote(PieceType.Rook);
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            return Promote(PieceType.Bishop);
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            return Promote(PieceType.Knight);
        }

        return false;
    }

    // 승급 시작
    public void Start(Piece piece)
    {
        if (piece.Type != PieceType.Pawn)
        {
            return;
        }

        int lastLine = piece.Team == Team.White ? board.Size - 1 : 0;
        if (piece.Z != lastLine)
        {
            return;
        }

        state.waitPromotion = true;
        state.promotionPawn = piece as Pawn;
        log?.Invoke("Promotion: Q / R / B / N");
    }

    // 승급 완료
    public bool Promote(PieceType type)
    {
        if (!state.waitPromotion || state.promotionPawn == null || state.gameOver)
        {
            return false;
        }
        if (type != PieceType.Queen && type != PieceType.Rook && type != PieceType.Bishop && type != PieceType.Knight) return false;

        int x = state.promotionPawn.X;
        int z = state.promotionPawn.Z;
        Team team = state.promotionPawn.Team;

        Piece newPiece = board.SpawnPiece(type, team, x, z);
        if (newPiece == null)
        {
            log?.Invoke("Promotion piece is unavailable. Choose another piece.");
            return false;
        }
        board.RemovePiece(state.promotionPawn);
        newPiece.SetState(x, z, true);

        state.waitPromotion = false;
        state.promotionPawn = null;

        log?.Invoke($"{team} promoted to {type}");
        return true;
    }
}
