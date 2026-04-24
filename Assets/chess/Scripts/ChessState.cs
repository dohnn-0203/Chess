using UnityEngine;

// 게임 상태
public class ChessState
{
    public Team turn = Team.White;
    public bool gameOver;
    public bool canEnPassant;
    public Vector2Int enPassantTile;
    public Vector2Int enPassantPawn;
    public bool waitPromotion;
    public Pawn promotionPawn;
}
