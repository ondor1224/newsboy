using UnityEngine;

public enum ObstacleType { Low, High, High_L, High_R }

public class Obstacle : MonoBehaviour
{
    public ObstacleType type;
    public float speed;

    public void SetupObstacleAsset()
    {
        // 장애물 외형 설정 (나중에 구현)
    }

    void Update()
    {
        transform.Translate(Vector3.back * speed * Time.deltaTime);
        if (transform.position.z < -15f)
        {
            Destroy(gameObject);
        }
    }
}