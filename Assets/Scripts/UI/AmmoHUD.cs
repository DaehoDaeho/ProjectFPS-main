using UnityEngine;
using TMPro;

/// <summary>
/// "탄창/예비" 형식으로 탄약을 표시.
/// </summary>
public class AmmoHUD : MonoBehaviour
{
    public TextMeshProUGUI label;

    private void Awake()
    {
        if (label == null)
        {
            label = GetComponent<TextMeshProUGUI>();
        }
    }

    public void SetAmmo(int mag, int reserve)
    {
        if (label == null)
        {
            return;
        }

        label.text = $"{mag} / {reserve}";
    }
}
