using UnityEngine;
using TMPro;                      // TMP 사용 시
using UnityEngine.UI;            // 기본 Text 사용 시
using System.Collections;

/// <summary>
/// Encounter HUD 전체 컨트롤러.
/// - 웨이브, 적 카운터, 메시지를 표시/애니메이션.
/// - EnemyEncounterZone의 UnityEvent를 구독해 갱신.
/// </summary>
public class EncounterHUDController : MonoBehaviour
{
    [Header("References")]
    public EnemyEncounterZone zone;                  // 이벤트를 발행하는 전투 구역
    public CanvasGroup panelWaveGroup;               // 웨이브 패널(CanvasGroup)
    public RectTransform panelWaveTransform;         // 웨이브 패널 트랜스폼(스케일 펄스)
    public CanvasGroup panelObjectiveGroup;          // 오브젝티브 패널(CanvasGroup)
    public TMP_Text waveLabelTMP;                    // "Wave 1" (TMP)
    public TMP_Text enemyCounterTMP;                 // "Enemies 3 / 7" (TMP)
    public TMP_Text toastTMP;                        // 토스트 문구 (TMP)

    public Text waveLabelText;                       // TMP 미사용 시 기본 Text(선택)
    public Text enemyCounterText;                    // TMP 미사용 시 기본 Text(선택)
    public Text toastText;                           // TMP 미사용 시 기본 Text(선택)

    [Header("Appearance")]
    public string waveFormat = "Wave {0}";           // 웨이브 텍스트 서식
    public string enemyCountFormat = "Enemies {0} / {1}";
    public float fadeSpeed = 4.0f;                   // 패널 페이드 속도
    public float pulseScale = 1.12f;                 // 웨이브 갱신 시 잠깐 커지는 배율
    public float pulseDuration = 0.18f;              // 펄스 지속 시간(초)
    public float toastDuration = 1.0f;               // 토스트 노출 유지시간(초)

    // 내부 상태
    private Coroutine fadeWaveCo;                    // 웨이브 패널 페이드 코루틴
    private Coroutine pulseWaveCo;                   // 웨이브 패널 펄스 코루틴
    private Coroutine toastCo;                       // 토스트 코루틴

    private void OnEnable()
    {
        if (zone != null)
        {
            zone.onEncounterStarted.AddListener(OnEncounterStarted);
            zone.onEncounterCompleted.AddListener(OnEncounterCompleted);
            zone.onWaveStarted.AddListener(OnWaveStarted);
            zone.onEnemyAliveChanged.AddListener(OnAliveChanged);
            zone.onEnemyTotalChanged.AddListener(OnTotalChanged);
        }
    }

    private void OnDisable()
    {
        if (zone != null)
        {
            zone.onEncounterStarted.RemoveListener(OnEncounterStarted);
            zone.onEncounterCompleted.RemoveListener(OnEncounterCompleted);
            zone.onWaveStarted.RemoveListener(OnWaveStarted);
            zone.onEnemyAliveChanged.RemoveListener(OnAliveChanged);
            zone.onEnemyTotalChanged.RemoveListener(OnTotalChanged);
        }
    }

    /// <summary>
    /// 인카운터 시작: 패널 페이드 인, Wave/Counter 초기 표시.
    /// </summary>
    private void OnEncounterStarted()
    {
        UpdateWaveLabel(zone.GetCurrentWaveIndex());
        UpdateEnemyCounter(zone.GetAliveEnemies(), zone.GetTotalEnemiesThisWave());
        PlayFade(panelWaveGroup, 1.0f);
        PlayFade(panelObjectiveGroup, 1.0f);
        ShowToast("Encounter Started");
    }

    /// <summary>
    /// 웨이브 시작: 라벨 갱신, 스케일 펄스, 토스트.
    /// </summary>
    private void OnWaveStarted(int waveIndex)
    {
        UpdateWaveLabel(waveIndex);
        UpdateEnemyCounter(zone.GetAliveEnemies(), zone.GetTotalEnemiesThisWave());
        PlayPulse(panelWaveTransform);
        ShowToast($"Wave {waveIndex + 1} 시작");
    }

    /// <summary>
    /// 살아있는 적 수 변경: 카운터 즉시 갱신.
    /// </summary>
    private void OnAliveChanged(int alive)
    {
        UpdateEnemyCounter(alive, zone.GetTotalEnemiesThisWave());
    }

    /// <summary>
    /// 이번 웨이브 총 소환 수 변경: 카운터 즉시 갱신.
    /// </summary>
    private void OnTotalChanged(int total)
    {
        UpdateEnemyCounter(zone.GetAliveEnemies(), total);
    }

    /// <summary>
    /// 인카운터 완료: 토스트 출력 후 HUD 페이드 아웃.
    /// </summary>
    private void OnEncounterCompleted()
    {
        ShowToast("Encounter Clear!");
        PlayFade(panelWaveGroup, 0.0f);
        // Objective 패널은 ObjectiveController와 협의, 여기선 유지
    }

    // ===== 표시 유틸 =====

    private void UpdateWaveLabel(int waveIndex)
    {
        int shown = waveIndex + 1;
        string text = string.Format(waveFormat, shown);

        if (waveLabelTMP != null)
        {
            waveLabelTMP.text = text;
        }
        if (waveLabelText != null)
        {
            waveLabelText.text = text;
        }
    }

    private void UpdateEnemyCounter(int alive, int total)
    {
        string text = string.Format(enemyCountFormat, alive, total);

        if (enemyCounterTMP != null)
        {
            enemyCounterTMP.text = text;
        }
        if (enemyCounterText != null)
        {
            enemyCounterText.text = text;
        }
    }

    private void ShowToast(string message)
    {
        if (toastCo != null)
        {
            StopCoroutine(toastCo);
            toastCo = null;
        }
        toastCo = StartCoroutine(CoToast(message));
    }

    // ===== 애니메이션(코루틴) =====

    private void PlayFade(CanvasGroup group, float target)
    {
        if (group == null)
        {
            return;
        }

        if (fadeWaveCo != null)
        {
            StopCoroutine(fadeWaveCo);
            fadeWaveCo = null;
        }

        fadeWaveCo = StartCoroutine(CoFade(group, target));
    }

    private IEnumerator CoFade(CanvasGroup group, float target)
    {
        float t = 0.0f;
        float start = group.alpha;

        while (t < 1.0f)
        {
            t = t + Time.deltaTime * fadeSpeed;
            if (t > 1.0f)
            {
                t = 1.0f;
            }

            float a = Mathf.Lerp(start, target, t);
            group.alpha = a;

            yield return null;
        }
    }

    private void PlayPulse(RectTransform target)
    {
        if (target == null)
        {
            return;
        }

        if (pulseWaveCo != null)
        {
            StopCoroutine(pulseWaveCo);
            pulseWaveCo = null;
        }

        pulseWaveCo = StartCoroutine(CoPulse(target));
    }

    private IEnumerator CoPulse(RectTransform target)
    {
        Vector3 original = target.localScale;     // 원래 스케일
        Vector3 peak = original * pulseScale;     // 커지는 목표 스케일
        float half = pulseDuration * 0.5f;        // 절반 시간

        float t = 0.0f;
        while (t < half)
        {
            t = t + Time.deltaTime;
            float k = t / half;
            if (k > 1.0f)
            {
                k = 1.0f;
            }
            target.localScale = Vector3.Lerp(original, peak, k);
            yield return null;
        }

        t = 0.0f;
        while (t < half)
        {
            t = t + Time.deltaTime;
            float k = t / half;
            if (k > 1.0f)
            {
                k = 1.0f;
            }
            target.localScale = Vector3.Lerp(peak, original, k);
            yield return null;
        }

        target.localScale = original;
    }

    private IEnumerator CoToast(string message)
    {
        if (toastTMP != null)
        {
            toastTMP.text = message;
        }
        if (toastText != null)
        {
            toastText.text = message;
        }

        float t = 0.0f;
        float showTime = toastDuration;

        // 간단한 나타남/유지/사라짐(0.25/유지/0.25)
        float fadeHalf = 0.25f;

        // 나타남
        float a = 0.0f;
        while (t < fadeHalf)
        {
            t = t + Time.deltaTime;
            float k = t / fadeHalf;
            if (k > 1.0f)
            {
                k = 1.0f;
            }
            a = Mathf.Lerp(0.0f, 1.0f, k);
            SetToastAlpha(a);
            yield return null;
        }

        // 유지
        float wait = showTime;
        while (wait > 0.0f)
        {
            wait = wait - Time.deltaTime;
            yield return null;
        }

        // 사라짐
        t = 0.0f;
        while (t < fadeHalf)
        {
            t = t + Time.deltaTime;
            float k = t / fadeHalf;
            if (k > 1.0f)
            {
                k = 1.0f;
            }
            a = Mathf.Lerp(1.0f, 0.0f, k);
            SetToastAlpha(a);
            yield return null;
        }

        SetToastAlpha(0.0f);
    }

    private void SetToastAlpha(float alpha)
    {
        if (toastTMP != null)
        {
            Color c = toastTMP.color;
            c.a = alpha;
            toastTMP.color = c;
        }
        if (toastText != null)
        {
            Color c2 = toastText.color;
            c2.a = alpha;
            toastText.color = c2;
        }
    }
}
