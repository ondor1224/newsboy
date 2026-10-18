using UnityEngine;

public class player : MonoBehaviour
{
    [Header("이동 및 점프 설정")]
    // forwardSpeed 변수는 전진하지 않으므로 제거했습니다.
    public float sideSpeed = 6f;      // 좌우 이동 속도
    public float xLimit = 4.5f;       // 좌우 도로 이탈 방지 범위
    public float jumpForce = 6f;      // 점프 힘

    // 보스 공격 판정(회피)을 위한 필수 속성
    public bool IsJumping { get; private set; }
    public bool IsSliding { get; private set; }

    private Collider col;
    public Bounds ColliderBounds
    {
        get { return col != null ? col.bounds : new Bounds(); }
    }

    private Rigidbody rb;
    private bool isGrounded = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    private void Start()
    {
        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (GameManager.Instance != null && (!GameManager.Instance.isPlaying || GameManager.Instance.isGameEnded))
            return;

        // 1. 좌우 입력 받기
        float moveX = Input.GetAxisRaw("Horizontal");

        // 2. 점프
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }

        // 3. 슬라이딩 (아래 화살표를 누르고 있는 동안)
        if (Input.GetKeyDown(KeyCode.DownArrow) && isGrounded)
        {
            IsSliding = true;
        }
        if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            IsSliding = false;
        }

        // 4. 전진 코드를 없애고 좌우 이동만 적용
        Vector3 move = Vector3.right * (moveX * sideSpeed);
        transform.Translate(move * Time.deltaTime, Space.World);

        // 5. 도로 밖으로 나가지 않게 X좌표 제한
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -xLimit, xLimit);
        transform.position = pos;
    }

    private void Jump()
    {
        if (rb != null)
        {
            isGrounded = false;
            IsJumping = true; // 점프 상태 켜기
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        isGrounded = true;
        IsJumping = false; // 땅에 닿으면 점프 상태 끄기

        if (collision.gameObject.CompareTag("Obstacle"))
        {
            if (GameManager.Instance != null)
                GameManager.Instance.TryUseShield();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Coin"))
        {
            if (GameManager.Instance != null)
                GameManager.Instance.AddCoin(1);
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("Shield"))
        {
            if (GameManager.Instance != null)
                GameManager.Instance.AddShield(1);
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("Obstacle"))
        {
            if (GameManager.Instance != null)
                GameManager.Instance.TryUseShield();
        }
    }
}