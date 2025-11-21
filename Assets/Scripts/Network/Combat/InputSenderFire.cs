using UnityEngine;

/// <summary>
/// 좌클릭 시 서버에 "FIRE" 명령을 보낸다.
/// - 서버가 sim.yaw/pitch로 판정하므로 여기서는 추가 데이터 생략.
/// - 발사 연출(머즐/반동/셰이크)은 즉시 로컬에서 재생한다.
/// </summary>
public class InputSenderFire : MonoBehaviour
{
    public float localFireCooldown = 0.08f;    // 로컬 피드백 쿨다운(서버 쿨다운과 유사하게)
    public MuzzleFlash muzzleFlash;            // 머즐 플래시 연출 컴포넌트(옵션)
    public CameraRecoil cameraRecoil;          // 카메라 반동 컴포넌트(옵션)
    public ScreenShake screenShake;            // 스크린 셰이크 컴포넌트(옵션)

    private float lastLocalFireTime;           // 마지막 로컬 발사 시간(연출 중복 방지)

    private void Update()
    {
        bool pressed = Input.GetMouseButtonDown(0); // 좌클릭 1회 트리거
        if (pressed == true)
        {
            TryFire();
        }
    }

    private void TryFire()
    {
        float now = Time.time;
        if (now < lastLocalFireTime + localFireCooldown)
        {
            return;
        }
        lastLocalFireTime = now;

        // 1) 서버에 FIRE 전송
        if (NetworkRunner.instance != null)
        {
            bool isClient = NetworkRunner.instance.IsClientConnected();
            bool isServer = NetworkRunner.instance.IsServerRunning();

            if (isClient == true)
            {
                NetworkRunner.instance.ClientSendLine("FIRE|");
            }
            else if (isServer == true)
            {
                // 호스트 전용(클라가 아닌 서버 단독 테스트일 때)
                NetworkRunner.instance.ServerInjectCommand(0, "FIRE", "");
            }
        }

        // 2) 로컬 연출
        if (muzzleFlash != null)
        {
            muzzleFlash.PlayOnce();
        }
        if (cameraRecoil != null)
        {
            cameraRecoil.Kick(2.2f, 0.6f); // (상하, 좌우) 반동 세기 예시
        }
        if (screenShake != null)
        {
            screenShake.ShakeOnce(0.08f, 0.12f); // (세기, 지속시간)
        }
    }
}
