using UnityEngine;

public class QualityPresetApplier : MonoBehaviour
{
    public bool showToast = true; // 콘솔 표시 여부.


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ApplyQuality(0);
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            ApplyQuality(1);
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            ApplyQuality(2);
        }
    }


    private void ApplyQuality(int index)
    {
        index = Mathf.Clamp(index, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(index, true);
        if (showToast == true)
        {
            Debug.Log($"[Quality] Set to {QualitySettings.names[index]}");
        }
    }
}