using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// StepEventSource가 내는 스텝 이벤트를 받아서
/// 표면 탐지 -> 오디오 선택/재생 -> 파티클 스폰 -> 카메라 임펄스 호출
/// </summary>
[DisallowMultipleComponent]
public class FootstepRouter : MonoBehaviour
{
    [Header("Refs")]
    public FootstepSurfaceDetector surfaceDetector; // 발밑 표면 판단.
    public AudioSource audioSource;                 // 재생기(플레이어에 붙이거나 3D 오디오로)
    public SimpleCameraImpulse cameraImpulse;       // 카메라 흔들기(선택)

    [Header("Audio Library")]
    public List<AudioClip> concreteClips = new List<AudioClip>();
    public List<AudioClip> dirtClips = new List<AudioClip>();
    public List<AudioClip> woodClips = new List<AudioClip>();
    public List<AudioClip> metalClips = new List<AudioClip>();
    public List<AudioClip> waterClips = new List<AudioClip>();
    public float volumeMin = 0.8f;
    public float volumeMax = 1.0f;
    public float pitchMin = 0.95f;
    public float pitchMax = 1.05f;

    [Header("VFX")]
    public ParticleSystem concreteVfxPrefab;
    public ParticleSystem dirtVfxPrefab;
    public ParticleSystem woodVfxPrefab;
    public ParticleSystem metalVfxPrefab;
    public ParticleSystem waterVfxPrefab;

    [Header("VFX Options")]
    public float vfxUpOffset = 0.05f;  // 지면에서 살짝 위로.
    public bool parentToWorld = true;  // 부모 없음(월드)로 스폰.

    // ==== 외부에서 연결할 스텝 이벤트 진입점 ====
    public void OnStepLeft()
    {
        HandleStep();
    }

    public void OnStepRight()
    {
        HandleStep();
    }

    // ==== 내부 ====
    private void HandleStep()
    {
        if (surfaceDetector == null)
        {
            return;
        }

        SurfaceType type;
        Vector3 point;
        Vector3 normal;

        bool ok = surfaceDetector.TryGetSurface(out type, out point, out normal);
        if (ok == false)
        {
            // 표면을 못 찾으면 최소한 오디오는 기본값으로 처리.
            type = SurfaceType.Concrete;
            point = transform.position;
            normal = Vector3.up;
        }

        PlayFootstepAudio(type);
        SpawnFootstepVfx(type, point, normal);
        PulseCameraImpulse();
    }

    private void PlayFootstepAudio(SurfaceType type)
    {
        Debug.Log("Audio Ground Type = " + type);

        if (audioSource == null)
        {
            return;
        }

        List<AudioClip> bank = GetClipBank(type);
        if (bank == null)
        {
            return;
        }
        if (bank.Count <= 0)
        {
            return;
        }

        // 랜덤으로 하나 선택 + 볼륨/피치 살짝 랜덤.
        int idx = Random.Range(0, bank.Count);
        AudioClip clip = bank[idx];

        float vol = Random.Range(volumeMin, volumeMax);
        float pit = Random.Range(pitchMin, pitchMax);

        audioSource.pitch = pit;
        audioSource.PlayOneShot(clip, vol);
    }

    private void SpawnFootstepVfx(SurfaceType type, Vector3 point, Vector3 normal)
    {
        Debug.Log("Vfx Ground Type = " + type);

        ParticleSystem prefab = GetVfxPrefab(type);
        if (prefab == null)
        {
            return;
        }

        Vector3 spawnPos = point + normal * vfxUpOffset;
        Quaternion spawnRot = Quaternion.LookRotation(Vector3.ProjectOnPlane(normal, Vector3.up), normal);

        if (parentToWorld == true)
        {
            ParticleSystem vfx = Instantiate(prefab, spawnPos, spawnRot);
            // 자동 파괴가 프리팹에 설정되어 있지 않다면 여기서 수명 후 Destroy를 고려.
            if(vfx != null)
            {
                Destroy(vfx.gameObject, 0.5f);
            }
        }
        else
        {
            ParticleSystem vfx = Instantiate(prefab, spawnPos, spawnRot, null);
            if (vfx != null)
            {
                Destroy(vfx.gameObject, 0.5f);
            }
        }
    }

    private void PulseCameraImpulse()
    {
        if (cameraImpulse != null)
        {
            cameraImpulse.Pulse();
        }
    }

    private List<AudioClip> GetClipBank(SurfaceType type)
    {
        if (type == SurfaceType.Concrete)
        {
            return concreteClips;
        }
        if (type == SurfaceType.Dirt)
        {
            return dirtClips;
        }
        if (type == SurfaceType.Wood)
        {
            return woodClips;
        }
        if (type == SurfaceType.Metal)
        {
            return metalClips;
        }
        if (type == SurfaceType.Water)
        {
            return waterClips;
        }
        return concreteClips;
    }

    private ParticleSystem GetVfxPrefab(SurfaceType type)
    {
        if (type == SurfaceType.Concrete)
        {
            return concreteVfxPrefab;
        }
        if (type == SurfaceType.Dirt)
        {
            return dirtVfxPrefab;
        }
        if (type == SurfaceType.Wood)
        {
            return woodVfxPrefab;
        }
        if (type == SurfaceType.Metal)
        {
            return metalVfxPrefab;
        }
        if (type == SurfaceType.Water)
        {
            return waterVfxPrefab;
        }
        return concreteVfxPrefab;
    }
}
