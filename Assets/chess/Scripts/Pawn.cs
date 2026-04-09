using System.Collections.Generic;
using UnityEngine;

public class Pawn : Piece
{
    // 이동 가능한 칸 반환
    public List<Vector2Int> GetMoves(Piece[,] board, int size)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        int dir = Team == Team.White ? 1 : -1;

        int nextZ = Z + dir;

        if (InRange(X, nextZ, size) && board[X, nextZ] == null)
        {
            moves.Add(new Vector2Int(X, nextZ));

            int twoZ = Z + dir * 2;
            if (!HasMoved && InRange(X, twoZ, size) && board[X, twoZ] == null)
            {
                moves.Add(new Vector2Int(X, twoZ));
            }
        }

        return moves;
    }

    // 범위 확인
    private bool InRange(int x, int z, int size)
    {
        return x >= 0 && x < size && z >= 0 && z < size;
    }
}