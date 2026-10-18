using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerController : MonoBehaviour
{
    [Header("이동 및 점프 설정")]
    public float forwardSpeed = 8f;   // 앞으로 달리는 속도
    public float sideSpeed = 6f;      // 좌우 이동 속도
    public float xLimit = 4.5f;       // 좌우 도로 이탈 방지 범위
    public float jumpForce = 6f;      // 점프 힘

    private Rigidbody rb;
    private bool isGrounded = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        // 씬 시작 시 멈춤 방지
        Time.timeScale = 1f;
    }

    private void Update()
    {
        // GameManager가 있고 게임오버 상태일 때만 멈춤 (GameManager가 없어도 움직임)
        if (GameManager.Instance != null && !GameManager.Instance.isPlaying)
            return;

        // 1. 좌우 입력 받기 (신버전 InputSystem & 기본 Input 동시 지원)
        float moveX = 0f;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) moveX = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) moveX = 1f;

            if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded)
            {
                Jump();
            }
        }
#else
        moveX = Input.GetAxisRaw("Horizontal");
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }
#endif

        // 2. 앞으로 전진 + 좌우 이동
        Vector3 move = Vector3.forward * forwardSpeed + Vector3.right * (moveX * sideSpeed);
        transform.Translate(move * Time.deltaTime, Space.World);

        // 3. 도로 밖으로 나가지 않게 X좌표 제한
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -xLimit, xLimit);
        transform.position = pos;
    }

    private void Jump()
    {
        if (rb != null)
        {
            isGrounded = false;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        isGrounded = true;

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