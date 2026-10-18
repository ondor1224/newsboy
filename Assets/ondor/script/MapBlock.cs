using UnityEngine;

public class MapBlock : MonoBehaviour
{
    void Update()
    {
        // 게임 오버 상태일 때는 배경 이동 멈춤
        if (GameManager.Instance != null && (!GameManager.Instance.isPlaying || GameManager.Instance.isGameEnded))
            return;

        // IntegratedSpawner가 설정한 전체 이동 속도를 가져와서 배경을 뒤로 이동
        float currentSpeed = 10f; // 기본값
        if (IntegratedSpawner.Instance != null)
        {
            currentSpeed = IntegratedSpawner.Instance.globalMoveSpeed;
        }

        // -Z 방향(플레이어 쪽)으로 이동
        transform.Translate(Vector3.back * currentSpeed * Time.deltaTime, Space.World);
    }
}