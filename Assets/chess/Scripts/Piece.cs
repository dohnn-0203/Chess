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

    public virtual void Init(Team team, PieceType type, int x, int z)
    {
        Team = team;
        Type = type;
        X = x;
        Z = z;
        HasMoved = false;
    }

    public void SetPos(int x, int z)
    {
        X = x;
        Z = z;
        HasMoved = true;
    }
}
