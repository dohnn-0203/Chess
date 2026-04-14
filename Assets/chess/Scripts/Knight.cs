using System.Collections.Generic;
using UnityEngine;

public class Knight : Piece
{
    private static readonly Vector2Int[] dirs =
    {
        new Vector2Int(-2, -1),
        new Vector2Int(-2, 1),
        new Vector2Int(-1, -2),
        new Vector2Int(-1, 2),
        new Vector2Int(1, -2),
        new Vector2Int(1, 2),
        new Vector2Int(2, -1),
        new Vector2Int(2, 1)
    };

    // 이동 가능한 칸
    public List<Vector2Int> GetMoves(Piece[,] board, int size)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        for (int i = 0; i < dirs.Length; i++)
        {
            int x = X + dirs[i].x;
            int z = Z + dirs[i].y;

            if (!InRange(x, z, size))
            {
                continue;
            }

            Piece target = board[x, z];

            if (target == null || target.Team != Team)
            {
                moves.Add(new Vector2Int(x, z));
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