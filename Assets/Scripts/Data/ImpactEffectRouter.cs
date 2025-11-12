using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// RaycastHit 정보를 받아 표면별 임팩트 이펙트/데칼/사운드를 발생.
/// SurfaceMaterial을 재사용.
/// </summary>
public class ImpactEffectRouter : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;             // 재생기(없으면 생략)
    public AudioClip concreteClip;
    public AudioClip dirtClip;
    public AudioClip woodClip;
    public AudioClip metalClip;
    public AudioClip waterClip;

    [Header("VFX")]
    public ParticleSystem concreteVfx;
    public ParticleSystem dirtVfx;
    public ParticleSystem woodVfx;
    public ParticleSystem metalVfx;
    public ParticleSystem waterVfx;

    [Header("Decals")]
    public DecalPool decalPool;                 // 데칼 풀(없으면 스킵)
    public GameObject concreteDecal;
    public GameObject dirtDecal;
    public GameObject woodDecal;
    public GameObject metalDecal;
    public GameObject waterDecal;

    [Header("Options")]
    public float decalNormalOffset = 0.002f;    // 표면에서 살짝 띄우기.
    public float vfxNormalOffset = 0.01f;       // VFX도 표면에서 살짝.
    public bool parentDecalToHit = true;        // 움직이는 표면이면 부모로 붙이기.

    public void SpawnImpact(RaycastHit hit)
    {
        // 1) 표면 타입 판정.
        SurfaceType type = SurfaceType.Concrete;
        SurfaceMaterial sm = hit.collider.GetComponent<SurfaceMaterial>();
        if (sm != null)
        {
            type = sm.surfaceType;
        }

        // 2) 오디오.
        AudioClip clip = GetClip(type);
        if (audioSource != null)
        {
            if (clip != null)
            {
                audioSource.PlayOneShot(clip, 1.0f);
            }
        }

        // 3) VFX
        ParticleSystem vfx = GetVfx(type);
        if (vfx != null)
        {
            Quaternion rot = Quaternion.LookRotation(Vector3.ProjectOnPlane(hit.normal, Vector3.up), hit.normal);
            Vector3 pos = hit.point + hit.normal * vfxNormalOffset;
            Instantiate(vfx, pos, rot);
        }

        // 4) Decal (풀 사용)
        GameObject decalPrefab = GetDecal(type);
        if (decalPrefab != null)
        {
            if (decalPool != null)
            {
                Quaternion rot = Quaternion.LookRotation(-hit.normal, Vector3.up);
                Vector3 pos = hit.point + hit.normal * decalNormalOffset;
                GameObject decal = decalPool.Spawn(decalPrefab, pos, rot);
                if (parentDecalToHit == true)
                {
                    decal.transform.SetParent(hit.collider.transform, true);
                }
            }
        }
    }

    private AudioClip GetClip(SurfaceType t)
    {
        if (t == SurfaceType.Concrete)
        {
            return concreteClip;
        }
        if (t == SurfaceType.Dirt)
        {
            return dirtClip;
        }
        if (t == SurfaceType.Wood)
        {
            return woodClip;
        }
        if (t == SurfaceType.Metal)
        {
            return metalClip;
        }
        if (t == SurfaceType.Water)
        {
            return waterClip;
        }
        return concreteClip;
    }

    private ParticleSystem GetVfx(SurfaceType t)
    {
        if (t == SurfaceType.Concrete)
        {
            return concreteVfx;
        }
        if (t == SurfaceType.Dirt)
        {
            return dirtVfx;
        }
        if (t == SurfaceType.Wood)
        {
            return woodVfx;
        }
        if (t == SurfaceType.Metal)
        {
            return metalVfx;
        }
        if (t == SurfaceType.Water)
        {
            return waterVfx;
        }
        return concreteVfx;
    }

    private GameObject GetDecal(SurfaceType t)
    {
        if (t == SurfaceType.Concrete)
        {
            return concreteDecal;
        }
        if (t == SurfaceType.Dirt)
        {
            return dirtDecal;
        }
        if (t == SurfaceType.Wood)
        {
            return woodDecal;
        }
        if (t == SurfaceType.Metal)
        {
            return metalDecal;
        }
        if (t == SurfaceType.Water)
        {
            return waterDecal;
        }
        return concreteDecal;
    }
}
