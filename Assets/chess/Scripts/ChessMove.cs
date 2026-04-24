using UnityEngine;

// 이동 정보
public struct ChessMove
{
    public Vector2Int to;
    public bool castle;
    public int rookFrom;
    public int rookTo;
    public bool enPassant;
    public Vector2Int enPassantPos;

    // 이동 만들기
    public static ChessMove New(int x, int z)
    {
        ChessMove move = new ChessMove();
        move.to = new Vector2Int(x, z);
        move.enPassantPos = new Vector2Int(-1, -1);
        return move;
    }
}
