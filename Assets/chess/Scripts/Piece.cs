using UnityEngine;

public enum Team
{
    White,
    Black
}

public enum PieceType
{
    Pawn,
    Rook,
    Knight
}

public class Piece : MonoBehaviour
{
    public Team Team { get; private set; }
    public PieceType Type { get; private set; }

    public int X { get; private set; }
    public int Z { get; private set; }
    public bool HasMoved { get; private set; }

    // 기본 정보 설정
    public virtual void Init(Team team, PieceType type, int x, int z)
    {
        Team = team;
        Type = type;
        X = x;
        Z = z;
        HasMoved = false;
    }

    // 위치 갱신
    public void SetPos(int x, int z)
    {
        X = x;
        Z = z;
        HasMoved = true;
    }
}