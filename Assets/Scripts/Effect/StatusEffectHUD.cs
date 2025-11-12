using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 현재 활성 상태효과를 간단한 아이콘 리스트로 표시.
/// - Horizontal Layout 안에 '아이템 슬롯 프리팹'을 복제.
/// - 각 슬롯: Image(아이콘) + TextMeshProUGUI(남은 시간, 스택)
/// </summary>
public class StatusEffectHUD : MonoBehaviour
{
    [Header("Refs")]
    public StatusEffectHost host;           // 대상 호스트.
    public RectTransform content;           // 슬롯들이 배치될 부모(수평 레이아웃 권장)
    public GameObject slotPrefab;           // 슬롯 프리팹(이미지+텍스트)

    private readonly List<GameObject> slots = new List<GameObject>(); // 생성된 슬롯.

    private void Awake()
    {
        if (host == null)
        {
            host = FindAnyObjectByType<StatusEffectHost>();
        }
    }

    private void LateUpdate()
    {
        if (host == null)
        {
            return;
        }

        if(slotPrefab == null)
        {
            return;
        }

        List<StatusEffectBase> list = host.GetActiveEffects();

        // 슬롯 수 맞추기.
        while (slots.Count < list.Count)
        {
            GameObject s = Instantiate(slotPrefab, content);
            slots.Add(s);
        }
        while (slots.Count > list.Count)
        {
            int last = slots.Count - 1;
            Destroy(slots[last]);
            slots.RemoveAt(last);
        }

        // 데이터 바인딩.
        for (int i = 0; i < list.Count; i = i + 1)
        {
            StatusEffectBase e = list[i];
            GameObject s = slots[i];

            Image iconImg = s.GetComponentInChildren<Image>();
            if (iconImg != null)
            {
                iconImg.sprite = e.icon;
                iconImg.enabled = e.icon != null;
            }

            TextMeshProUGUI txt = s.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                // 남은 시간(정수) + 선택: 스택.
                int left = Mathf.CeilToInt(e.GetTimeLeft());
                string extra = "";

                StatusEffect_DOT dot = e as StatusEffect_DOT;
                if (dot != null)
                {
                    extra = $" x{dot.GetStacks()}";
                }

                txt.text = $"{e.effectName} {left}s{extra}";
            }
        }
    }
}
