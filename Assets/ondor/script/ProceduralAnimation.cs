using UnityEngine;

public class ProceduralAnimation : MonoBehaviour
{
    [Header("달리기 설정")]
    public float moveSpeed = 15f;     // 움직임 속도
    public float bounceHeight = 0.15f;// 통통 튀는 높이
    public float tiltAngle = 10f;     // 좌우로 기우뚱하는 각도

    private Vector3 startPos;
    private Vector3 startRot; // ★ 원래 회전값을 저장할 변수 추가

    void Start()
    {
        // 시작할 때의 원래 위치와 회전값을 기억해 둡니다.
        startPos = transform.localPosition;
        startRot = transform.localEulerAngles;
    }

    void Update()
    {
        // Mathf.Sin을 이용해 시간의 흐름에 따라 부드러운 곡선 값을 만듭니다.
        float bounce = Mathf.Abs(Mathf.Sin(Time.time * moveSpeed)) * bounceHeight;
        float tilt = Mathf.Sin(Time.time * (moveSpeed / 2)) * tiltAngle;

        // 1. 위아래로 통통 튀기
        transform.localPosition = new Vector3(startPos.x, startPos.y + bounce, startPos.z);

        // 2. 원래 회전값(startRot)을 유지한 채로 Z축에만 기우뚱(tilt) 효과 더하기
        transform.localRotation = Quaternion.Euler(startRot.x, startRot.y, startRot.z + tilt);
    }
}