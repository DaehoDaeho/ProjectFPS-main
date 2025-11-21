using UnityEngine;

/// <summary>
/// 발사 시 카메라에 반동(pitch/yaw)을 더하고 시간이 지나면 부드럽게 복귀.
/// - FirstPersonCameraRig의 yaw/pitch를 직접 누적하는 대신 별도 오프셋으로 더하는 방식.
/// - 여기서는 간단히 '피벗의 로컬 회전'에 작은 오프셋을 합산한다.
/// </summary>
public class CameraRecoil : MonoBehaviour
{
    public Transform cameraPivot;       // 회전을 줄 피벗(FirstPersonCameraRig.cameraPivot 권장)
    public float returnSpeed = 8.0f;    // 복귀 속도(높을수록 빨리 돌아옴)

    private float recoilPitch;          // 누적 반동(상하)
    private float recoilYaw;            // 누적 반동(좌우)

    private void Update()
    {
        // 시간이 지날수록 원점으로 점진 복귀
        float dt = Time.deltaTime;

        if (Mathf.Abs(recoilPitch) > 0.0001f)
        {
            recoilPitch = Mathf.Lerp(recoilPitch, 0.0f, dt * returnSpeed);
        }
        else
        {
            recoilPitch = 0.0f;
        }

        if (Mathf.Abs(recoilYaw) > 0.0001f)
        {
            recoilYaw = Mathf.Lerp(recoilYaw, 0.0f, dt * returnSpeed);
        }
        else
        {
            recoilYaw = 0.0f;
        }

        if (cameraPivot != null)
        {
            // 기존 회전 + recoil 오프셋을 더하는 구조라면,
            // 여기서는 오프셋 전용 자식 피벗을 쓰는 편이 안전.
            cameraPivot.localRotation = Quaternion.Euler(recoilPitch, recoilYaw, 0.0f);
        }
    }

    public void Kick(float pitchAmount, float yawAmount)
    {
        // 발사 시 상하/좌우 반동을 누적
        recoilPitch = recoilPitch + pitchAmount;
        recoilYaw = recoilYaw + Random.Range(-yawAmount, yawAmount);
    }
}
