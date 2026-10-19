using UnityEngine;

public class player : MonoBehaviour
{
    [Header("차선 대쉬 이동 설정 (-5, 0, 5)")]
    public float laneDistance = 5f;
    public float dashSpeed = 18f;
    private int currentLane = 1;

    [Header("점프 및 슬라이딩 설정")]
    public float jumpForce = 7f;
    public bool shrinkVisualOnSlide = true;

    public bool IsJumping { get; private set; }
    public bool IsSliding { get; private set; }

    private Collider col;
    public Bounds ColliderBounds => col != null ? col.bounds : new Bounds();

    private Rigidbody rb;
    private bool isGrounded = true;
    private float startZ;

    private Vector3 originalScale;
    private BoxCollider boxCol;
    private Vector3 originalBoxSize, originalBoxCenter;
    private CapsuleCollider capsuleCol;
    private float originalCapsuleHeight;
    private Vector3 originalCapsuleCenter;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        startZ = transform.position.z;
        originalScale = transform.localScale;

        boxCol = col as BoxCollider;
        if (boxCol != null)
        {
            originalBoxSize = boxCol.size;
            originalBoxCenter = boxCol.center;
        }

        capsuleCol = col as CapsuleCollider;
        if (capsuleCol != null)
        {
            originalCapsuleHeight = capsuleCol.height;
            originalCapsuleCenter = capsuleCol.center;
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;
        currentLane = 1;
    }

    private void Update()
    {
        if (GameManager.Instance != null && (!GameManager.Instance.isPlaying || GameManager.Instance.isGameEnded))
            return;

        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            if (currentLane > 0) currentLane--;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            if (currentLane < 2) currentLane++;
        }

        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            && isGrounded && !IsSliding)
        {
            Jump();
        }

        if ((Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) && isGrounded)
        {
            if (!IsSliding) StartSlide();
        }
        else
        {
            if (IsSliding) StopSlide();
        }

        float targetX = (currentLane - 1) * laneDistance;
        Vector3 pos = transform.position;
        pos.x = Mathf.Lerp(pos.x, targetX, Time.deltaTime * dashSpeed);

        if (Mathf.Abs(pos.x - targetX) < 0.05f)
        {
            pos.x = targetX;
        }

        pos.z = startZ;
        transform.position = pos;
    }

    private void Jump()
    {
        if (rb != null)
        {
            isGrounded = false;
            IsJumping = true;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void StartSlide()
    {
        IsSliding = true;

        if (boxCol != null)
        {
            boxCol.size = new Vector3(originalBoxSize.x, originalBoxSize.y * 0.45f, originalBoxSize.z);
            boxCol.center = new Vector3(originalBoxCenter.x, originalBoxCenter.y - (originalBoxSize.y * 0.275f), originalBoxCenter.z);
        }
        else if (capsuleCol != null)
        {
            capsuleCol.height = originalCapsuleHeight * 0.45f;
            capsuleCol.center = new Vector3(originalCapsuleCenter.x, originalCapsuleCenter.y - (originalCapsuleHeight * 0.275f), originalCapsuleCenter.z);
        }

        if (shrinkVisualOnSlide)
        {
            transform.localScale = new Vector3(originalScale.x, originalScale.y * 0.5f, originalScale.z);
        }
    }

    private void StopSlide()
    {
        IsSliding = false;

        if (boxCol != null)
        {
            boxCol.size = originalBoxSize;
            boxCol.center = originalBoxCenter;
        }
        else if (capsuleCol != null)
        {
            capsuleCol.height = originalCapsuleHeight;
            capsuleCol.center = originalCapsuleCenter;
        }

        if (shrinkVisualOnSlide)
        {
            transform.localScale = originalScale;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            Vector3 normal = collision.contacts[0].normal;

            if (normal.y > 0.5f)
            {
                isGrounded = true;
                IsJumping = false;
            }
            else
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.TryUseShield();

                collision.collider.enabled = false;
            }
        }
        else
        {
            isGrounded = true;
            IsJumping = false;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.contacts.Length > 0 && collision.contacts[0].normal.y > 0.5f)
        {
            isGrounded = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Coin"))
        {
            if (GameManager.Instance != null)
                GameManager.Instance.AddCoin(1);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayCoinSound();

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
            other.enabled = false;
        }
    }
}