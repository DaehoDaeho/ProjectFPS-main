using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 간단 Objective 표시 컨트롤러.
/// - 현재 목표 텍스트를 보여주고, 완료 시 토스트/체크 애니.
/// - Encounter 시작/완료에 맞춰 기본 목표를 세팅.
/// </summary>
[DisallowMultipleComponent]
public class ObjectiveController : MonoBehaviour
{
    [Header("References")]
    public EnemyEncounterZone zone;             // 이벤트 제공자
    public CanvasGroup objectiveGroup;          // 패널 페이드
    public TMP_Text objectiveTMP;               // 목표 문장(TMP)
    public Text objectiveText;                  // TMP 미사용 시 Text

    [Header("Appearance")]
    public string startObjective = "Clear Zone";
    public string afterClearObjective = "Move To Next Zone";
    public float fadeSpeed = 4.0f;

    private Coroutine fadeCo;                   // 페이드 코루틴

    private void OnEnable()
    {
        if (zone != null)
        {
            zone.onEncounterStarted.AddListener(OnEncounterStarted);
            zone.onEncounterCompleted.AddListener(OnEncounterCompleted);
        }
    }

    private void OnDisable()
    {
        if (zone != null)
        {
            zone.onEncounterStarted.RemoveListener(OnEncounterStarted);
            zone.onEncounterCompleted.RemoveListener(OnEncounterCompleted);
        }
    }

    private void OnEncounterStarted()
    {
        SetObjective(startObjective);
        PlayFade(1.0f);
    }

    private void OnEncounterCompleted()
    {
        SetObjective(afterClearObjective);
        PlayFade(1.0f);
    }

    /// <summary>
    /// 텍스트를 설정한다(TMP/기본 Text 모두 지원).
    /// </summary>
    public void SetObjective(string message)
    {
        if (objectiveTMP != null)
        {
            objectiveTMP.text = message;
        }
        if (objectiveText != null)
        {
            objectiveText.text = message;
        }
    }

    private void PlayFade(float target)
    {
        if (objectiveGroup == null)
        {
            return;
        }

        if (fadeCo != null)
        {
            StopCoroutine(fadeCo);
            fadeCo = null;
        }

        fadeCo = StartCoroutine(CoFade(target));
    }

    private IEnumerator CoFade(float target)
    {
        float t = 0.0f;
        float start = objectiveGroup.alpha;

        while (t < 1.0f)
        {
            t = t + Time.deltaTime * fadeSpeed;
            if (t > 1.0f)
            {
                t = 1.0f;
            }
            float a = Mathf.Lerp(start, target, t);
            objectiveGroup.alpha = a;
            yield return null;
        }
    }
}
