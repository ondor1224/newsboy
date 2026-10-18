using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("따라갈 대상 (비워두면 자동으로 Player를 찾음)")]
    public Transform target;

    [Header("카메라 기본 거리 및 각도")]
    public Vector3 offset = new Vector3(0f, 3.8f, -6.5f); // 플레이어 뒤쪽 & 위쪽 거리
    public float lookDownAngle = 15f;                     // 아래를 내려다보는 각도

    [Header("톰 골드런 카메라 무빙 설정")]
    public float xSmoothSpeed = 7f;    // 좌우 추적 속도 (플레이어 대쉬보다 살짝 느려야 역동적임!)
    [Range(0.5f, 1f)]
    public float xFollowRatio = 0.8f;  // 차선 이동 시 카메라가 따라가는 비율 (0.8이면 양 끝 벽이 안 잘리고 도로가 예쁘게 보임)
    public float ySmoothSpeed = 4f;    // 점프 및 마차 위 탑승 시 위아래로 부드럽게 따라가는 속도

    private void Start()
    {
        // 인스펙터에서 target을 연결 안 했어도 자동으로 player를 찾아줌
        if (target == null)
        {
            player p = FindObjectOfType<player>();
            if (p != null) target = p.transform;
        }

        // 시작 시 카메라 초기 위치 및 각도 세팅
        if (target != null)
        {
            transform.position = new Vector3(
                target.position.x * xFollowRatio + offset.x,
                target.position.y + offset.y,
                target.position.z + offset.z
            );
            transform.rotation = Quaternion.Euler(lookDownAngle, 0f, 0f);
        }
    }

    // ★ 중요: 카메라는 반드시 LateUpdate에서 움직여야 캐릭터가 덜덜 떨리지 않음!
    private void LateUpdate()
    {
        if (target == null) return;

        // 1. 좌우(X) 이동: 플레이어가 -5, 5로 대쉬하면 카메라는 살짝 안쪽(-4, 4)까지 반 박자 늦게 따라감
        float targetX = (target.position.x * xFollowRatio) + offset.x;

        // 2. 상하(Y) 이동: 점프할 때 화면이 정신없이 흔들리지 않게 묵직하고 부드럽게 따라감
        float targetY = target.position.y + offset.y;

        // 3. 앞뒤(Z) 위치: 일정한 거리 유지
        float targetZ = target.position.z + offset.z;

        Vector3 currentPos = transform.position;
        currentPos.x = Mathf.Lerp(currentPos.x, targetX, Time.deltaTime * xSmoothSpeed);
        currentPos.y = Mathf.Lerp(currentPos.y, targetY, Time.deltaTime * ySmoothSpeed);
        currentPos.z = targetZ;

        transform.position = currentPos;
    }
}