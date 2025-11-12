using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostVolumeController : MonoBehaviour
{
    [Header("Refs")]
    public Volume globalVolume;
    public Slider bloomIntensitySlider;
    public Slider exposureSlider;
    public Slider dofFocusSlider;

    Bloom bloom;
    ColorAdjustments color;
    DepthOfField dof;

    float defaultBloomIntensity = 2f;
    float defaultExposure = 0f;
    float defaultFocusDistance = 5f;

    private void Awake()
    {
        // "Global Volume" 자동 연결 시도(없으면 인스펙터로 수동 연결)
        if (globalVolume == null)
        {
            GameObject gv = GameObject.Find("Global Volume");
            if (gv != null)
            {
                globalVolume = gv.GetComponent<Volume>();
            }
        }

        // 컴포넌트 캐시 및 기본값 저장
        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out bloom);
            globalVolume.profile.TryGet(out color);
            globalVolume.profile.TryGet(out dof);

            if (bloom != null && bloom.intensity.overrideState)
            {
                defaultBloomIntensity = bloom.intensity.value;
            }

            if (color != null && color.postExposure.overrideState)
            {
                defaultExposure = color.postExposure.value;
            }

            if (dof != null && dof.focusDistance.overrideState)
            {
                defaultFocusDistance = dof.focusDistance.value;
            }
        }

        // 슬라이더 범위/초기값/리스너
        if (bloomIntensitySlider != null)
        {
            bloomIntensitySlider.minValue = 0f;
            bloomIntensitySlider.maxValue = 5f;
            bloomIntensitySlider.value = defaultBloomIntensity;
            bloomIntensitySlider.onValueChanged.AddListener(OnBloomChanged);
        }
        if (exposureSlider != null)
        {
            exposureSlider.minValue = -2f;
            exposureSlider.maxValue = 2f;
            exposureSlider.value = defaultExposure;
            exposureSlider.onValueChanged.AddListener(OnExposureChanged);
        }
        if (dofFocusSlider != null)
        {
            dofFocusSlider.minValue = 0.5f;
            dofFocusSlider.maxValue = 20f;
            dofFocusSlider.value = defaultFocusDistance;
            dofFocusSlider.onValueChanged.AddListener(OnFocusChanged);
        }

        // 시작 시 슬라이더 값을 한번 적용
        ApplyAll();
    }

    private void Update()
    {
        // 단축키: 1=Bloom 토글, 2=Exposure 토글, 3=DoF 토글, R=리셋
        if (Input.GetKeyDown(KeyCode.Alpha1) == true)
        {
            ToggleBloom();
        }

        if (Input.GetKeyDown(KeyCode.Alpha2) == true)
        {
            ToggleExposure();
        }

        if (Input.GetKeyDown(KeyCode.Alpha3) == true)
        {
            ToggleDoF();
        }

        if (Input.GetKeyDown(KeyCode.R) == true)
        {
            ResetDefaults();
        }
    }

    void ToggleBloom()
    {
        if (bloom != null)
        {
            bloom.active = !bloom.active;
        }
    }

    void ToggleExposure()
    {
        if (color != null)
        {
            color.active = !color.active;
        }
    }

    void ToggleDoF()
    { 
        if (dof != null)
        { 
            dof.active = !dof.active;
        }
    }

    void ResetDefaults()
    {
        if (bloom != null)
        {
            bloom.intensity.Override(defaultBloomIntensity);
            if (bloomIntensitySlider != null) { bloomIntensitySlider.value = defaultBloomIntensity; }
        }
        if (color != null)
        {
            color.postExposure.Override(defaultExposure);
            if (exposureSlider != null) { exposureSlider.value = defaultExposure; }
        }
        if (dof != null)
        {
            dof.focusDistance.Override(defaultFocusDistance);
            if (dofFocusSlider != null) { dofFocusSlider.value = defaultFocusDistance; }
        }
    }

    void OnBloomChanged(float v)
    {
        if (bloom != null)
        {
            bloom.intensity.Override(v);
        }
    }

    void OnExposureChanged(float v)
    {
        if (color != null)
        {
            color.postExposure.Override(v);
        }
    }

    void OnFocusChanged(float v)
    {
        if (dof != null)
        {
            dof.focusDistance.Override(v);
        }
    }

    void ApplyAll()
    {
        if (bloom != null && bloomIntensitySlider != null)
        {
            bloom.intensity.Override(bloomIntensitySlider.value);
        }

        if (color != null && exposureSlider != null)
        {
            color.postExposure.Override(exposureSlider.value);
        }

        if (dof != null && dofFocusSlider != null)
        {
            dof.focusDistance.Override(dofFocusSlider.value);
        }
    }
}
