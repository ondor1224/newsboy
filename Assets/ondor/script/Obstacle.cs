using UnityEngine;

public enum ObstacleType { Low, High, High_L, High_R }

public class Obstacle : MonoBehaviour
{
    public ObstacleType type;
    public float speed = 8f;

    [Header("장애물 높이 설정 (바닥에 안 닿으면 이 숫자를 조절!)")]
    public float lowObstacleY = 0f;    // 일반 장애물(마차 등) 바닥 높이 (기본 0)
    public float highObstacleY = 1.6f; // 숙여서 피하는 공중 장애물 높이

    void Start()
    {
        SetupObstacleAsset();
    }

    public void SetupObstacleAsset()
    {
        Vector3 pos = transform.position;

        if (type == ObstacleType.High || type == ObstacleType.High_L || type == ObstacleType.High_R)
        {
            // 숙여서 피하는 장애물은 공중에 띄움
            pos.y = highObstacleY;
        }
        else
        {
            // 일반 장애물은 바닥에 딱 붙임
            pos.y = lowObstacleY;
        }

        transform.position = pos;
    }

    void Update()
    {
        if (GameManager.Instance != null && (!GameManager.Instance.isPlaying || GameManager.Instance.isGameEnded))
            return;

        transform.Translate(Vector3.back * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Kill"))
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Kill"))
        {
            Destroy(gameObject);
        }
    }
}