using System.Collections.Generic;
using UnityEngine;

// 폰 이동
public class Pawn : Piece
{
    // 이동 구하기
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

        int leftX = X - 1;
        if (InRange(leftX, nextZ, size))
        {
            Piece leftPiece = board[leftX, nextZ];
            if (leftPiece != null && leftPiece.Team != Team)
            {
                moves.Add(new Vector2Int(leftX, nextZ));
            }
        }

        int rightX = X + 1;
        if (InRange(rightX, nextZ, size))
        {
            Piece rightPiece = board[rightX, nextZ];
            if (rightPiece != null && rightPiece.Team != Team)
            {
                moves.Add(new Vector2Int(rightX, nextZ));
            }
        }

        return moves;
    }

    // 범위 체크
    private bool InRange(int x, int z, int size)
    {
        return x >= 0 && x < size && z >= 0 && z < size;
    }
}
