using UnityEngine;

public class Coin : MonoBehaviour
{
    [Header("이동 및 회전 설정")]
    public float speed = 10f;          // 스포너가 지정해주는 전체 속도와 동기화됨
    public float rotateSpeed = 180f;  // 회전 속도

    private float fixedY;              // Y축 높이 고정용

    private void Awake()
    {
        // 1. 코인의 콜라이더는 플레이어가 통과하면서 먹을 수 있게 Trigger 유지
        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (var col in cols)
        {
            col.isTrigger = true;
        }

        // 2. 중력 및 물리 튕김 방지
        Rigidbody[] rbs = GetComponentsInChildren<Rigidbody>();
        foreach (var rb in rbs)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
        }
    }

    private void Start()
    {
        // 처음 생성된 높이(Y)를 기억해서 밑으로 안 떨어지게 잠금
        fixedY = transform.position.y;
    }

    void Update()
    {
        if (GameManager.Instance != null && (!GameManager.Instance.isPlaying || GameManager.Instance.isGameEnded))
            return;

        // 앞으로(-Z 방향) 일정하게 이동하되 Y높이는 철저히 고정
        Vector3 pos = transform.position;
        pos.y = fixedY;
        pos.z -= speed * Time.deltaTime;
        transform.position = pos;

        // 제자리 회전
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime, Space.World);
    }

    // ★ 플레이어가 닿았을 때 처리는 player.cs의 OnTriggerEnter가 담당하므로 여기서는 코인을 먹지 않음!
    // 오직 장애물처럼 'Kill' 태그를 가진 벽(뒤쪽 벽)에 닿았을 때만 사라짐!
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