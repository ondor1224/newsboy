using System.Collections;
using UnityEngine;

public class player : MonoBehaviour
{
    public float speed = 6f;
    public float jumpPower = 7.5f;
    public float limitX = 3.5f;
    public float slideDuration = 0.6f;
    public float slideHeightRatio = 0.45f;

    private float hAxis;
    private bool isJump;
    private bool isSlide;
    private float originHeight;
    private Vector3 originCenter;
    private Rigidbody rigid;
    private CapsuleCollider col;
    private Coroutine slideCo;

    public bool IsJumping => isJump;
    public bool IsSliding => isSlide;
    public Bounds ColliderBounds => col != null ? col.bounds : new Bounds(transform.position, Vector3.one);

    void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        if (col != null)
        {
            originHeight = col.height;
            originCenter = col.center;
        }
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.isPlaying)
        {
            hAxis = 0f;
            return;
        }

        hAxis = Input.GetKey(KeyCode.A) ? -1f : (Input.GetKey(KeyCode.D) ? 1f : Input.GetAxisRaw("Horizontal"));

        if (Input.GetButtonDown("Jump") && !isJump)
        {
            if (isSlide) StopSlide();
            rigid.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
            isJump = true;
        }

        if ((Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) && !isJump && !isSlide)
        {
            if (slideCo != null) StopCoroutine(slideCo);
            slideCo = StartCoroutine(SlideRoutine());
        }
    }

    void FixedUpdate()
    {
        if (GameManager.Instance == null || !GameManager.Instance.isPlaying) return;
        Vector3 next = rigid.position + new Vector3(hAxis * speed * Time.fixedDeltaTime, 0f, 0f);
        next.x = Mathf.Clamp(next.x, -limitX, limitX);
        rigid.MovePosition(next);
    }

    IEnumerator SlideRoutine()
    {
        isSlide = true;
        if (col != null)
        {
            float h = originHeight * slideHeightRatio;
            col.height = h;
            col.center = new Vector3(originCenter.x, originCenter.y - (originHeight - h) * 0.5f, originCenter.z);
        }
        yield return new WaitForSeconds(slideDuration);
        StopSlide();
    }

    void StopSlide()
    {
        if (slideCo != null) StopCoroutine(slideCo);
        if (col != null)
        {
            col.height = originHeight;
            col.center = originCenter;
        }
        isSlide = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Floor")) isJump = false;
    }
}