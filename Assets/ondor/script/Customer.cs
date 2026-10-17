using UnityEngine;

public enum CustomerSide
{
    Left,
    Right
}

public class Customer : MonoBehaviour
{
    public CustomerSide side;
    public bool isHandled = false;
    public float moveSpeed = 5f;

    void Update()
    {
        if (isHandled) return;

        // 손님이 플레이어 쪽으로 다가오는 기본 이동 로직
        transform.Translate(Vector3.back * moveSpeed * Time.deltaTime);

        // 화면 뒤로 넘어가면 오브젝트 삭제
        if (transform.position.z < -15f)
        {
            Destroy(gameObject);
        }
    }
}