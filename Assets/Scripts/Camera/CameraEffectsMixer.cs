using UnityEngine;

public interface ICameraEffect
{
    Vector3 CurrentPositionOffset { get; }
    Vector3 CurrentRotationOffsetEuler { get; }
    float CurrentFovOffset { get; }
}

public class CameraEffectsMixer : MonoBehaviour
{
    [Header("Target")]
    public Camera targetCamera;                 // 결과를 적용할 카메라.

    [Header("Effects")]
    public MonoBehaviour[] effectBehaviours;    // ICameraEffect 구현 컴포넌트들.

    [Header("Master Intensity")]
    [Range(0f, 3f)] public float positionIntensity = 1.25f; // 위치 오프셋 전체 배율.
    [Range(0f, 3f)] public float rotationIntensity = 1.25f; // 회전 오프셋 전체 배율.
    [Range(0f, 3f)] public float fovIntensity = 1.10f;      // FOV 오프셋 전체 배율.    

    private Vector3 baseLocalPosition;          // 기준 로컬 위치.
    private Quaternion baseLocalRotation;       // 기준 로컬 회전.
    private float baseFov;                      // 기준 FOV.

    private ICameraEffect[] effects;            // 캐스팅된 효과 목록.

    private bool setFOV = true;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        if (targetCamera == null)
        {
            Debug.LogError("CameraEffectsMixer: targetCamera가 필요합니다.");
        }

        baseLocalPosition = transform.localPosition;
        baseLocalRotation = transform.localRotation;

        if (targetCamera != null)
        {
            baseFov = targetCamera.fieldOfView;
        }

        if (effectBehaviours != null)
        {
            effects = new ICameraEffect[effectBehaviours.Length];

            for (int i = 0; i < effectBehaviours.Length; i++)
            {
                ICameraEffect eff = effectBehaviours[i] as ICameraEffect; // 효과 캐스팅 결과.
                if (eff != null)
                {
                    effects[i] = eff;
                }
            }
        }
    }

    private void LateUpdate()
    {
        Vector3 posOffset = Vector3.zero;       // 위치 오프셋 누적.
        Vector3 rotEulerOffset = Vector3.zero;  // 회전 오프셋 누적.
        float fovOffset = 0f;                   // FOV 오프셋 누적.

        if (effects != null)
        {
            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i] != null)
                {
                    posOffset += effects[i].CurrentPositionOffset;
                    rotEulerOffset += effects[i].CurrentRotationOffsetEuler;
                    fovOffset += effects[i].CurrentFovOffset;
                }
            }
        }

        // ★ 전역 강도 적용
        posOffset *= positionIntensity;
        rotEulerOffset *= rotationIntensity;
        fovOffset *= fovIntensity;

        transform.localPosition = baseLocalPosition + posOffset;

        Quaternion rotOffsetQuat = Quaternion.Euler(rotEulerOffset); // 오일러 -> 쿼터니언
        transform.localRotation = baseLocalRotation * rotOffsetQuat;

        if (targetCamera != null && setFOV == true)
        {
            targetCamera.fieldOfView = baseFov + fovOffset;
        }
    }

    public void SetFOV(bool value)
    {
        setFOV = value;
    }
}
