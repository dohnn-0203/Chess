using UnityEngine;

public class Tile : MonoBehaviour
{
    private int x;
    private int z;

    // 좌표 저장
    public void Init(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    // 클릭 처리
    private void OnMouseDown()
    {
        Debug.Log($"타일 클릭: ({x}, {z})");
    }
}