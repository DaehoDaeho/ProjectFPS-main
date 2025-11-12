using UnityEngine;

/// <summary>
/// 발사 시 카메라 Pivot에 짧은 반동을 부여(위로 살짝 튕김 + 좌우 약간 랜덤).
/// </summary>
public class RecoilApplier : MonoBehaviour
{
    public Transform cameraPivot;      // 회전 적용 지점(보통 CameraPivot)
    public float pitchKick = 1.2f;     // 위로 튀는 각도.
    public float yawJitter = 0.4f;     // 좌우 랜덤.
    public float returnTime = 0.12f;   // 원위치 복귀 시간(초)

    private float timeLeft;            // 남은 반동 시간.
    private Quaternion baseLocalRot;   // 시작 회전.
    private Quaternion targetRot;      // 목표 회전.

    private void Awake()
    {
        if (cameraPivot == null)
        {
            cameraPivot = transform;
        }
        baseLocalRot = cameraPivot.localRotation;
        targetRot = baseLocalRot;
    }

    private void Update()
    {
        if (timeLeft > 0.0f)
        {
            float t = 1.0f - (timeLeft / returnTime); // 0→1
            float smooth = Mathf.SmoothStep(0.0f, 1.0f, t);
            cameraPivot.localRotation = Quaternion.Slerp(targetRot, baseLocalRot, smooth);

            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0.0f)
            {
                cameraPivot.localRotation = baseLocalRot;
            }
        }
    }

    public void Kick()
    {
        // 위로 pitchKick, 좌우는 랜덤.
        float yaw = Random.Range(-yawJitter, yawJitter);
        Quaternion kick = Quaternion.Euler(-pitchKick, yaw, 0.0f);

        baseLocalRot = cameraPivot.localRotation;
        targetRot = baseLocalRot * kick;
        timeLeft = returnTime;
    }
}
