using UnityEngine;

// 아이콘을 플레이어 자식이 아닌 별도로 관리
public class MiniMapPlayerIcon : MonoBehaviour
{
    private Transform player;
    
    private void Start()
    {
        player = PlayerStats.Instance.transform;
    }
    
    private void LateUpdate()
    {
        if (player != null)
        {
            transform.position = player.position;
            transform.rotation = Quaternion.Euler(90f, 0f, 0f); // 고정
        }
    }
}
