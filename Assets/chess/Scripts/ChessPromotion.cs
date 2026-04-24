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
            Promote(PieceType.Queen);
            return true;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            Promote(PieceType.Rook);
            return true;
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            Promote(PieceType.Bishop);
            return true;
        }

        if (Input.GetKeyDown(KeyCode.N))
        {
            Promote(PieceType.Knight);
            return true;
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
    private void Promote(PieceType type)
    {
        if (state.promotionPawn == null)
        {
            state.waitPromotion = false;
            return;
        }

        int x = state.promotionPawn.X;
        int z = state.promotionPawn.Z;
        Team team = state.promotionPawn.Team;

        board.RemovePiece(state.promotionPawn);

        Piece newPiece = board.SpawnPiece(type, team, x, z);
        if (newPiece == null)
        {
            newPiece = board.SpawnPiece(PieceType.Queen, team, x, z);
            type = PieceType.Queen;
        }

        if (newPiece != null)
        {
            newPiece.SetState(x, z, true);
        }

        state.waitPromotion = false;
        state.promotionPawn = null;

        log?.Invoke($"{team} promoted to {type}");
    }
}
