using UnityEngine;

public class LimbAnimator : MonoBehaviour
{
    [Header("팔다리 뼈대(Transform) 연결")]
    public Transform leftArm;
    public Transform rightArm;
    public Transform leftLeg;
    public Transform rightLeg;

    [Header("달리기 설정 (앞뒤 교차)")]
    public float runSpeed = 15f;      // 다리 구르는 속도
    public float swingAngle = 45f;    // 팔다리가 앞뒤로 흔들리는 각도

    [Header("점프 설정 (양옆 벌리기)")]
    public float spreadArmAngle = 60f; // 점프 시 팔 벌리는 각도
    public float spreadLegAngle = 30f; // 점프 시 다리 벌리는 각도
    public float lerpSpeed = 15f;      // 벌려지는(전환되는) 부드러운 속도

    // 각 관절의 원래 회전값을 저장할 변수
    private Quaternion laStart, raStart, llStart, rlStart;

    // 플레이어의 점프 상태를 알아오기 위한 변수
    private player playerScript;

    void Start()
    {
        // 시작할 때 각 뼈대의 원래 각도를 기억
        if (leftArm != null) laStart = leftArm.localRotation;
        if (rightArm != null) raStart = rightArm.localRotation;
        if (leftLeg != null) llStart = leftLeg.localRotation;
        if (rightLeg != null) rlStart = rightLeg.localRotation;

        // 같은 오브젝트(또는 부모)에 있는 player 스크립트를 자동으로 찾아서 연결합니다.
        playerScript = GetComponentInParent<player>();
    }

    void Update()
    {
        // 1. 점프 중일 때 (양옆으로 대자 뻗기)
        if (playerScript != null && playerScript.IsJumping)
        {
            // 옆으로 벌어지는 각도 계산 (축이 Z축이라고 가정)
            Quaternion laTarget = laStart * Quaternion.Euler(0, 0, spreadArmAngle);
            Quaternion raTarget = raStart * Quaternion.Euler(0, 0, -spreadArmAngle);
            Quaternion llTarget = llStart * Quaternion.Euler(0, 0, spreadLegAngle);
            Quaternion rlTarget = rlStart * Quaternion.Euler(0, 0, -spreadLegAngle);

            // Lerp를 사용해 확 꺾이지 않고 부드럽게 벌어지도록 만듭니다.
            if (leftArm != null) leftArm.localRotation = Quaternion.Lerp(leftArm.localRotation, laTarget, Time.deltaTime * lerpSpeed);
            if (rightArm != null) rightArm.localRotation = Quaternion.Lerp(rightArm.localRotation, raTarget, Time.deltaTime * lerpSpeed);
            if (leftLeg != null) leftLeg.localRotation = Quaternion.Lerp(leftLeg.localRotation, llTarget, Time.deltaTime * lerpSpeed);
            if (rightLeg != null) rightLeg.localRotation = Quaternion.Lerp(rightLeg.localRotation, rlTarget, Time.deltaTime * lerpSpeed);
        }
        // 2. 바닥을 달릴 때 (앞뒤로 교차하기)
        else
        {
            float wave = Mathf.Sin(Time.time * runSpeed) * swingAngle;

            Quaternion laTarget = laStart * Quaternion.Euler(wave, 0, 0);
            Quaternion raTarget = raStart * Quaternion.Euler(-wave, 0, 0);
            Quaternion llTarget = llStart * Quaternion.Euler(-wave, 0, 0);
            Quaternion rlTarget = rlStart * Quaternion.Euler(wave, 0, 0);

            // 점프하다가 착지하면 다시 부드럽게 달리기 모션으로 돌아옵니다.
            if (leftArm != null) leftArm.localRotation = Quaternion.Lerp(leftArm.localRotation, laTarget, Time.deltaTime * lerpSpeed);
            if (rightArm != null) rightArm.localRotation = Quaternion.Lerp(rightArm.localRotation, raTarget, Time.deltaTime * lerpSpeed);
            if (leftLeg != null) leftLeg.localRotation = Quaternion.Lerp(leftLeg.localRotation, llTarget, Time.deltaTime * lerpSpeed);
            if (rightLeg != null) rightLeg.localRotation = Quaternion.Lerp(rightLeg.localRotation, rlTarget, Time.deltaTime * lerpSpeed);
        }
    }
}