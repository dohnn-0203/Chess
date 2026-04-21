using UnityEngine;

public class Tile : MonoBehaviour
{
    private int x;
    private int z;
    private BoardManager board;

    public void Init(int x, int z, BoardManager board)
    {
        this.x = x;
        this.z = z;
        this.board = board;
    }

    public int X => x;
    public int Z => z;

    private void OnMouseDown()
    {
        board.SelectTile(this);
    }
}
