using UnityEngine;

/// <summary>
/// 뷰모델 총구에서 짧은 빛/파티클 효과를 재생.
/// - 간단히 Light를 켰다가 끄거나, ParticleSystem을 Play한다.
/// </summary>
public class MuzzleFlash : MonoBehaviour
{
    public Light flashLight;             // 총구 플래시로 쓸 라이트(옵션)
    public ParticleSystem particles;     // 파티클(옵션)
    public float lightOnDuration = 0.03f;// 라이트 켜둘 시간(초)

    private float lightOffTime;          // 라이트를 끌 시각

    private void Update()
    {
        if (flashLight != null)
        {
            if (flashLight.enabled == true && Time.time >= lightOffTime)
            {
                flashLight.enabled = false;
            }
        }
    }

    public void PlayOnce()
    {
        if (flashLight != null)
        {
            flashLight.enabled = true;
            lightOffTime = Time.time + lightOnDuration;
        }
        if (particles != null)
        {
            particles.Play();
        }
    }
}
