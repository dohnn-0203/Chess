using UnityEngine;

// 타일 정보
public class Tile : MonoBehaviour
{
    private int x;
    private int z;
    private BoardManager board;

    // 타일 초기화
    public void Init(int x, int z, BoardManager board)
    {
        this.x = x;
        this.z = z;
        this.board = board;
    }

    public int X => x;
    public int Z => z;

    // 클릭 처리
    private void OnMouseDown()
    {
        board.ClickTile(this);
    }
}
