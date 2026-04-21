using System.Collections.Generic;
using UnityEngine;

public class King : Piece
{
    private static readonly Vector2Int[] Directions =
    {
        new Vector2Int(-1, -1),
        new Vector2Int(-1, 0),
        new Vector2Int(-1, 1),
        new Vector2Int(0, -1),
        new Vector2Int(0, 1),
        new Vector2Int(1, -1),
        new Vector2Int(1, 0),
        new Vector2Int(1, 1)
    };

    public List<Vector2Int> GetMoves(Piece[,] board, int size)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        for (int i = 0; i < Directions.Length; i++)
        {
            int x = X + Directions[i].x;
            int z = Z + Directions[i].y;

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

    private bool InRange(int x, int z, int size)
    {
        return x >= 0 && x < size && z >= 0 && z < size;
    }
}
