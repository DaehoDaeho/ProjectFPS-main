using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 전체를 붉게 플래시 시키는 연출.
/// - Image(Color)의 알파 값을 CanvasGroup으로 제어하여 페이드 인/아웃
/// - 데미지 양에 비례한 강도 누적(상한 cap) 및 자연스러운 감쇠
/// - 싱글톤 한 개만 씬에 존재한다고 가정(간편 호출용)
/// </summary>
public class ScreenDamageOverlay : MonoBehaviour
{
    [Header("References")]
    public Image overlayImage;            // 전체 화면을 덮는 Image(빨강)
    public CanvasGroup canvasGroup;       // 알파 제어용 CanvasGroup(같은 오브젝트 권장)

    [Header("Appearance")]
    public Color overlayColor = new Color(1.0f, 0.0f, 0.0f, 1.0f); // 오버레이 색상(알파는 CanvasGroup이 제어)
    public float maxAlpha = 0.6f;        // 최대 알파(상한 캡, 0~1)
    public float hitBoost = 0.35f;       // 데미지 100 기준 가중치(상황에 맞게 조정)
    public float fadeOutSpeed = 2.5f;    // 초당 감쇠 속도(값이 클수록 빨리 사라짐)

    [Header("Optional Pulse")]
    public bool useQuickPulse = true;     // 피격 직후 아주 잠깐 알파를 살짝 올려서 '틱' 느낌
    public float pulseUpSpeed = 40.0f;    // 펄스 올라가는 속도(고속)
    public float pulseDuration = 0.05f;   // 펄스 유지 시간(초)

    // 싱글톤(간단 호출용)
    public static ScreenDamageOverlay instance;

    // 내부 상태
    private float targetAlpha;            // 현재 목표 알파(감쇠 대상)
    private float pulseTimer;             // 펄스 유지 타이머(>0이면 펄스 모드)

    private void Awake()
    {
        // 싱글톤 초기화(동일 타입이 여러 개면 첫 번째만 사용)
        if (instance == null)
        {
            instance = this;
        }

        if (overlayImage == null)
        {
            overlayImage = GetComponent<Image>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        // 초기 색/알파 셋업
        if (overlayImage != null)
        {
            overlayImage.color = overlayColor;
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.0f;
        }

        targetAlpha = 0.0f;
        pulseTimer = 0.0f;
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 펄스 처리: 피격 직후 잠깐 빠르게 치고 올라간 뒤
        // pulseDuration이 지나면 일반 감쇠로 복귀
        if (useQuickPulse == true)
        {
            if (pulseTimer > 0.0f)
            {
                pulseTimer = pulseTimer - dt;
                if (pulseTimer < 0.0f)
                {
                    pulseTimer = 0.0f;
                }
            }
        }

        // 현재 알파값 조회
        float current = 0.0f;
        if (canvasGroup != null)
        {
            current = canvasGroup.alpha;
        }

        // 목표 알파로 이동
        float next = current;

        // 펄스 중에는 빠르게 상향 보간
        if (pulseTimer > 0.0f)
        {
            float stepUp = pulseUpSpeed * dt;
            next = current + stepUp;
        }
        else
        {
            // 일반 감쇠: 목표 알파까지 선형 이동(감쇠 속도는 fadeOutSpeed)
            float stepDown = fadeOutSpeed * dt;
            next = Mathf.MoveTowards(current, targetAlpha, stepDown);
        }

        // 범위 클램프
        if (next < 0.0f)
        {
            next = 0.0f;
        }
        if (next > maxAlpha)
        {
            next = maxAlpha;
        }

        // 적용
        if (canvasGroup != null)
        {
            canvasGroup.alpha = next;
        }

        // 목표 알파도 천천히 0으로 향하게(지속 감쇠)
        targetAlpha = targetAlpha - fadeOutSpeed * dt;
        if (targetAlpha < 0.0f)
        {
            targetAlpha = 0.0f;
        }
    }

    /// <summary>
    /// 데미지 발생 시 호출: 데미지 양과 최대 체력으로 강도를 계산해 누적한다.
    /// damage: 이번에 받은 피해량(양수)
    /// maxHealth: 플레이어 최대 체력(강도 정규화에 사용)
    /// </summary>
    public void PlayDamageFlash(float damage, float maxHealth)
    {
        if (damage <= 0.0f)
        {
            return;
        }
        if (maxHealth <= 0.0f)
        {
            maxHealth = 100.0f;
        }

        // 데미지 비율
        float ratio = damage / maxHealth;

        // 새로 더해줄 목표 알파 증가량 계산
        // - hitBoost는 100% 데미지 가정 시 더해줄 양(디자이너 조정 포인트)
        float add = ratio * hitBoost;

        // 목표 알파 누적
        targetAlpha = targetAlpha + add;

        // 상한 캡
        if (targetAlpha > maxAlpha)
        {
            targetAlpha = maxAlpha;
        }

        // 펄스 트리거
        if (useQuickPulse == true)
        {
            pulseTimer = pulseDuration;
        }
    }

    /// <summary>
    /// 외부에서 강도를 직접 지정하고 싶을 때(0~1 입력) 호출.
    /// </summary>
    public void PlayDamageFlashNormalized(float normalized)
    {
        float n = normalized;
        if (n < 0.0f)
        {
            n = 0.0f;
        }
        if (n > 1.0f)
        {
            n = 1.0f;
        }

        // 정규화 값을 maxAlpha에 매핑.
        float add = n * hitBoost;
        targetAlpha = targetAlpha + add;

        if (targetAlpha > maxAlpha)
        {
            targetAlpha = maxAlpha;
        }

        if (useQuickPulse == true)
        {
            pulseTimer = pulseDuration;
        }
    }
}
