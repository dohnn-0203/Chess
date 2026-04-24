using System.Collections.Generic;
using UnityEngine;

// 이동 규칙
public class ChessRules
{
    private readonly BoardManager board;
    private readonly ChessState state;

    // 규칙 준비
    public ChessRules(BoardManager boardManager, ChessState gameState)
    {
        board = boardManager;
        state = gameState;
    }

    // 합법 수 구하기
    public List<ChessMove> GetLegalMoves(Piece piece)
    {
        List<ChessMove> result = new List<ChessMove>();
        List<ChessMove> list = GetMoves(piece, false);

        for (int i = 0; i < list.Count; i++)
        {
            Piece target = board.GetPiece(list[i].to.x, list[i].to.y);
            if (target != null && target.Team != piece.Team && target.Type == PieceType.King)
            {
                continue;
            }

            if (IsLegal(piece, list[i]))
            {
                result.Add(list[i]);
            }
        }

        return result;
    }

    // 체크 확인
    public bool IsCheck(Team team)
    {
        Piece king = FindKing(team);
        if (king == null)
        {
            return false;
        }

        return IsAttack(king.X, king.Z, Other(team));
    }

    // 이동 가능 확인
    public bool HasMove(Team team)
    {
        for (int x = 0; x < board.Size; x++)
        {
            for (int z = 0; z < board.Size; z++)
            {
                Piece piece = board.GetPiece(x, z);
                if (piece == null || piece.Team != team)
                {
                    continue;
                }

                if (GetLegalMoves(piece).Count > 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    // 이동 적용
    public void PlayMove(Piece piece, ChessMove move)
    {
        Piece target = move.enPassant
            ? board.GetPiece(move.enPassantPos.x, move.enPassantPos.y)
            : board.GetPiece(move.to.x, move.to.y);

        if (target != null && target.Team != piece.Team)
        {
            board.RemovePiece(target);
        }

        int fromX = piece.X;
        int fromZ = piece.Z;

        board.SetPiece(fromX, fromZ, null);

        if (move.castle)
        {
            Piece rook = board.GetPiece(move.rookFrom, fromZ);
            if (rook != null)
            {
                board.SetPiece(move.rookFrom, fromZ, null);
                board.MovePiece(rook, move.rookTo, fromZ);
            }
        }

        board.MovePiece(piece, move.to.x, move.to.y);
        UpdateEnPassant(piece, fromZ, move.to.y);
    }

    // 기본 수 만들기
    private List<ChessMove> GetMoves(Piece piece, bool attackOnly)
    {
        List<ChessMove> list = new List<ChessMove>();

        switch (piece.Type)
        {
            case PieceType.Pawn:
                AddPawnMoves(piece, list, attackOnly);
                break;
            case PieceType.Rook:
                AddLineMoves(piece, list, 1, 0);
                AddLineMoves(piece, list, -1, 0);
                AddLineMoves(piece, list, 0, 1);
                AddLineMoves(piece, list, 0, -1);
                break;
            case PieceType.Knight:
                AddKnightMoves(piece, list);
                break;
            case PieceType.Bishop:
                AddLineMoves(piece, list, 1, 1);
                AddLineMoves(piece, list, 1, -1);
                AddLineMoves(piece, list, -1, 1);
                AddLineMoves(piece, list, -1, -1);
                break;
            case PieceType.Queen:
                AddLineMoves(piece, list, 1, 0);
                AddLineMoves(piece, list, -1, 0);
                AddLineMoves(piece, list, 0, 1);
                AddLineMoves(piece, list, 0, -1);
                AddLineMoves(piece, list, 1, 1);
                AddLineMoves(piece, list, 1, -1);
                AddLineMoves(piece, list, -1, 1);
                AddLineMoves(piece, list, -1, -1);
                break;
            case PieceType.King:
                AddKingMoves(piece, list, attackOnly);
                break;
        }

        return list;
    }

    // 폰 이동
    private void AddPawnMoves(Piece piece, List<ChessMove> list, bool attackOnly)
    {
        int dir = piece.Team == Team.White ? 1 : -1;
        int nextZ = piece.Z + dir;

        if (attackOnly)
        {
            AddPawnAttack(piece.X - 1, nextZ, list);
            AddPawnAttack(piece.X + 1, nextZ, list);
            return;
        }

        if (board.IsInside(piece.X, nextZ) && board.GetPiece(piece.X, nextZ) == null)
        {
            list.Add(ChessMove.New(piece.X, nextZ));

            int twoZ = piece.Z + dir * 2;
            if (!piece.HasMoved && board.IsInside(piece.X, twoZ) && board.GetPiece(piece.X, twoZ) == null)
            {
                list.Add(ChessMove.New(piece.X, twoZ));
            }
        }

        AddPawnTake(piece, piece.X - 1, nextZ, list);
        AddPawnTake(piece, piece.X + 1, nextZ, list);

        if (!state.canEnPassant)
        {
            return;
        }

        if (nextZ != state.enPassantTile.y)
        {
            return;
        }

        if (Mathf.Abs(piece.X - state.enPassantTile.x) != 1)
        {
            return;
        }

        Piece enemyPawn = board.GetPiece(state.enPassantPawn.x, state.enPassantPawn.y);
        if (enemyPawn == null || enemyPawn.Team == piece.Team || enemyPawn.Type != PieceType.Pawn)
        {
            return;
        }

        ChessMove move = ChessMove.New(state.enPassantTile.x, state.enPassantTile.y);
        move.enPassant = true;
        move.enPassantPos = state.enPassantPawn;
        list.Add(move);
    }

    // 폰 공격 칸
    private void AddPawnAttack(int x, int z, List<ChessMove> list)
    {
        if (!board.IsInside(x, z))
        {
            return;
        }

        list.Add(ChessMove.New(x, z));
    }

    // 폰 잡기
    private void AddPawnTake(Piece piece, int x, int z, List<ChessMove> list)
    {
        if (!board.IsInside(x, z))
        {
            return;
        }

        Piece target = board.GetPiece(x, z);
        if (target != null && target.Team != piece.Team)
        {
            list.Add(ChessMove.New(x, z));
        }
    }

    // 나이트 이동
    private void AddKnightMoves(Piece piece, List<ChessMove> list)
    {
        Vector2Int[] dirs =
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

        for (int i = 0; i < dirs.Length; i++)
        {
            int x = piece.X + dirs[i].x;
            int z = piece.Z + dirs[i].y;

            if (!board.IsInside(x, z))
            {
                continue;
            }

            Piece target = board.GetPiece(x, z);
            if (target == null || target.Team != piece.Team)
            {
                list.Add(ChessMove.New(x, z));
            }
        }
    }

    // 킹 이동
    private void AddKingMoves(Piece piece, List<ChessMove> list, bool attackOnly)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dz = -1; dz <= 1; dz++)
            {
                if (dx == 0 && dz == 0)
                {
                    continue;
                }

                int x = piece.X + dx;
                int z = piece.Z + dz;
                if (!board.IsInside(x, z))
                {
                    continue;
                }

                Piece target = board.GetPiece(x, z);
                if (target == null || target.Team != piece.Team)
                {
                    list.Add(ChessMove.New(x, z));
                }
            }
        }

        if (attackOnly || piece.HasMoved || IsCheck(piece.Team))
        {
            return;
        }

        AddCastle(piece, list, true);
        AddCastle(piece, list, false);
    }

    // 캐슬링 체크
    private void AddCastle(Piece king, List<ChessMove> list, bool kingSide)
    {
        int rookX = kingSide ? board.Size - 1 : 0;
        Piece rook = board.GetPiece(rookX, king.Z);

        if (rook == null || rook.Team != king.Team || rook.Type != PieceType.Rook || rook.HasMoved)
        {
            return;
        }

        int step = kingSide ? 1 : -1;
        for (int x = king.X + step; x != rookX; x += step)
        {
            if (board.GetPiece(x, king.Z) != null)
            {
                return;
            }
        }

        int midX = king.X + step;
        int toX = king.X + step * 2;

        if (IsAttack(midX, king.Z, Other(king.Team)) || IsAttack(toX, king.Z, Other(king.Team)))
        {
            return;
        }

        ChessMove move = ChessMove.New(toX, king.Z);
        move.castle = true;
        move.rookFrom = rookX;
        move.rookTo = toX - step;
        list.Add(move);
    }

    // 직선 이동
    private void AddLineMoves(Piece piece, List<ChessMove> list, int dx, int dz)
    {
        int x = piece.X + dx;
        int z = piece.Z + dz;

        while (board.IsInside(x, z))
        {
            Piece target = board.GetPiece(x, z);

            if (target == null)
            {
                list.Add(ChessMove.New(x, z));
            }
            else
            {
                if (target.Team != piece.Team)
                {
                    list.Add(ChessMove.New(x, z));
                }

                break;
            }

            x += dx;
            z += dz;
        }
    }

    // 합법 수 체크
    private bool IsLegal(Piece piece, ChessMove move)
    {
        int fromX = piece.X;
        int fromZ = piece.Z;
        bool moved = piece.HasMoved;

        Piece taken = move.enPassant
            ? board.GetPiece(move.enPassantPos.x, move.enPassantPos.y)
            : board.GetPiece(move.to.x, move.to.y);

        Piece rook = null;
        bool rookMoved = false;

        board.SetPiece(fromX, fromZ, null);

        if (move.enPassant && taken != null)
        {
            board.SetPiece(move.enPassantPos.x, move.enPassantPos.y, null);
        }

        if (move.castle)
        {
            rook = board.GetPiece(move.rookFrom, fromZ);
            if (rook != null)
            {
                rookMoved = rook.HasMoved;
                board.SetPiece(move.rookFrom, fromZ, null);
                board.SetPiece(move.rookTo, fromZ, rook);
                rook.SetState(move.rookTo, fromZ, true);
            }
        }

        board.SetPiece(move.to.x, move.to.y, piece);
        piece.SetState(move.to.x, move.to.y, true);

        bool check = IsCheck(piece.Team);

        board.SetPiece(move.to.x, move.to.y, move.enPassant ? null : taken);
        board.SetPiece(fromX, fromZ, piece);
        piece.SetState(fromX, fromZ, moved);

        if (move.enPassant && taken != null)
        {
            board.SetPiece(move.enPassantPos.x, move.enPassantPos.y, taken);
        }

        if (move.castle && rook != null)
        {
            board.SetPiece(move.rookTo, fromZ, null);
            board.SetPiece(move.rookFrom, fromZ, rook);
            rook.SetState(move.rookFrom, fromZ, rookMoved);
        }

        return !check;
    }

    // 공격 확인
    private bool IsAttack(int x, int z, Team team)
    {
        for (int bx = 0; bx < board.Size; bx++)
        {
            for (int bz = 0; bz < board.Size; bz++)
            {
                Piece piece = board.GetPiece(bx, bz);
                if (piece == null || piece.Team != team)
                {
                    continue;
                }

                List<ChessMove> list = GetMoves(piece, true);
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].to.x == x && list[i].to.y == z)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    // 킹 찾기
    private Piece FindKing(Team team)
    {
        for (int x = 0; x < board.Size; x++)
        {
            for (int z = 0; z < board.Size; z++)
            {
                Piece piece = board.GetPiece(x, z);
                if (piece != null && piece.Team == team && piece.Type == PieceType.King)
                {
                    return piece;
                }
            }
        }

        return null;
    }

    // 앙파상 갱신
    private void UpdateEnPassant(Piece piece, int fromZ, int toZ)
    {
        state.canEnPassant = false;

        if (piece.Type != PieceType.Pawn)
        {
            return;
        }

        if (Mathf.Abs(toZ - fromZ) != 2)
        {
            return;
        }

        state.canEnPassant = true;
        state.enPassantTile = new Vector2Int(piece.X, (fromZ + toZ) / 2);
        state.enPassantPawn = new Vector2Int(piece.X, toZ);
    }

    // 상대 팀
    private Team Other(Team team)
    {
        return team == Team.White ? Team.Black : Team.White;
    }
}
