using UnityEngine;

// 말 기본값
public enum Team
{
    White,
    Black
}

public enum PieceType
{
    Pawn,
    Rook,
    Knight,
    Bishop,
    Queen,
    King
}

public class Piece : MonoBehaviour
{
    public Team Team { get; private set; }
    public PieceType Type { get; private set; }

    public int X { get; private set; }
    public int Z { get; private set; }
    public bool HasMoved { get; private set; }

    // 말 초기화
    public virtual void Init(Team team, PieceType type, int x, int z)
    {
        Team = team;
        Type = type;
        X = x;
        Z = z;
        HasMoved = false;
    }

    // 이동 위치 저장
    public void SetPos(int x, int z)
    {
        SetState(x, z, true);
    }

    // 상태 저장
    public void SetState(int x, int z, bool hasMoved)
    {
        X = x;
        Z = z;
        HasMoved = hasMoved;
    }
}
