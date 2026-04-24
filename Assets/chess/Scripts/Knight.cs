using System.Collections.Generic;
using UnityEngine;

// 나이트 이동
public class Knight : Piece
{
    private static readonly Vector2Int[] Dirs =
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

    // 이동 구하기
    public List<Vector2Int> GetMoves(Piece[,] board, int size)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        for (int i = 0; i < Dirs.Length; i++)
        {
            int x = X + Dirs[i].x;
            int z = Z + Dirs[i].y;

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
