using UnityEngine;

public class Tile : MonoBehaviour
{
    private int x;
    private int z;
    private BoardManager board;

    // 좌표와 보드 저장
    public void Init(int x, int z, BoardManager board)
    {
        this.x = x;
        this.z = z;
        this.board = board;
    }

    public int X => x;
    public int Z => z;

    // 클릭 전달
    private void OnMouseDown()
    {
        board.SelectTile(this);
    }
}