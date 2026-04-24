using System.Collections.Generic;
using UnityEngine;

// 퀸 이동
public class Queen : Piece
{
    // 이동 구하기
    public List<Vector2Int> GetMoves(Piece[,] board, int size)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        AddLine(moves, board, size, 1, 0);
        AddLine(moves, board, size, -1, 0);
        AddLine(moves, board, size, 0, 1);
        AddLine(moves, board, size, 0, -1);

        AddLine(moves, board, size, 1, 1);
        AddLine(moves, board, size, 1, -1);
        AddLine(moves, board, size, -1, 1);
        AddLine(moves, board, size, -1, -1);

        return moves;
    }

    // 방향 체크
    private void AddLine(List<Vector2Int> moves, Piece[,] board, int size, int dx, int dz)
    {
        int x = X + dx;
        int z = Z + dz;

        while (InRange(x, z, size))
        {
            Piece target = board[x, z];
            if (target == null)
            {
                moves.Add(new Vector2Int(x, z));
            }
            else
            {
                if (target.Team != Team)
                {
                    moves.Add(new Vector2Int(x, z));
                }

                break;
            }

            x += dx;
            z += dz;
        }
    }

    // 범위 체크
    private bool InRange(int x, int z, int size)
    {
        return x >= 0 && x < size && z >= 0 && z < size;
    }
}
