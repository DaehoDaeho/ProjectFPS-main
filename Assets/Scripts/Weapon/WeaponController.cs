using UnityEngine;
using System.Collections;

/// <summary>
/// 무기의 핵심 로직: 탄약, 연사 쿨다운, 재장전, ADS(FOV), 히트스캔 사격, 반동 이벤트.
/// 상태: Idle / Firing / Reloading / Ads(보조 상태 플래그).
/// </summary>
[DisallowMultipleComponent]
public class WeaponController : MonoBehaviour
{
    [Header("Refs")]
    public Camera playerCamera;                 // 레이캐스트/ADS FOV 기준.
    public ParticleSystem muzzleFlash;          // 총구 화염.
    public GameObject muzzleLight;
    public AudioSource audioSource;             // 무기 사운드.
    public Transform raycastOrigin;             // 레이 시작(카메라 중앙이 권장)
    public LayerMask hitMask;                   // 맞을 수 있는 레이어.
    public RecoilApplier recoil;                // 반동 적용(카메라 킥)
    public CrosshairUI crosshair;               // 크로스헤어 확산 반영.
    public AmmoHUD ammoHud;                     // 탄약 표시 TMP.

    public WeaponProjectileLauncher launcher;   // 총알 Prefab 발사를 위한 참조용.
    public bool useBulletPrefab;                // 총알 Prefab을 사용할지 여부.
    
    [Header("Ammo")]
    public int magSize = 30;                    // 탄창 크기.
    public int reserveAmmo = 90;                // 예비 탄약.
    public float reloadTime = 1.9f;             // 재장전 시간(초)
    public bool chamberedRound = true;          // 실탄 한 발 장전 방식(선택)

    [Header("Fire")]
    public float fireRate = 10.0f;              // 초당 발사수(10 → 0.1초 간격)
    public float damage = 20.0f;                // 히트 데미지(데모용)
    public float maxRange = 150.0f;             // 히트스캔 최대 사거리.
    public AudioClip shotSfx;                   // 발사음.
    public AudioClip drySfx;                    // 빈 탄창 클릭음.
    public Transform muzzleTransform;

    [Header("ADS")]
    public float adsFov = 55.0f;                // ADS 시 FOV
    public float hipFov = 70.0f;                // 허리쏘기 FOV
    public float adsBlendTime = 0.12f;          // FOV 전환 시간(초)
    public bool applyMouseSensitivityScale = false; // (선택) 마우스 감도 배율 적용.
    public float adsMouseScale = 0.8f;          // ADS 시 감도 배율.

    [Header("VFX")]
    public ParticleSystem hitVfxPrefab;         // 피격 스파크/먼지.
    public GameObject bulletDecalPrefab;        // 데칼(선택)

    public ImpactEffectRouter impactRouter;  // 표면 임팩트 라우터.
    public bool applyHitboxMultiplier = true; // 히트박스 배수 적용 여부.
    
    // 내부 상태
    private int ammoInMag;                      // 현재 탄창 잔탄.
    private float fireCooldown;                 // 발사 쿨다운 타이머.
    private bool isReloading;                   // 재장전 중 여부.
    private bool fireHeld;                      // 입력: 발사 누름.
    private bool adsHeld;                       // 입력: ADS 누름.
    private float fovVel;                       // FOV SmoothDamp 속도 캐시.

    private void Start()
    {
        ammoInMag = magSize; // 시작 시 가득 장전.
        if (playerCamera != null)
        {
            playerCamera.fieldOfView = hipFov;
        }
        UpdateAmmoHud();
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        // 1) FOV(ADS) 전환.
        UpdateAdsFov(dt);

        // 2) 재장전 중이면 발사 금지.
        if (isReloading == true)
        {
            // 쿨다운도 천천히 복귀(불필요한 잔상 방지)
            if (fireCooldown > 0.0f)
            {
                fireCooldown -= dt;
                if (fireCooldown < 0.0f)
                {
                    fireCooldown = 0.0f;
                }
            }
            return;
        }

        // 3) 발사 쿨다운 갱신.
        if (fireCooldown > 0.0f)
        {
            fireCooldown -= dt;
            if (fireCooldown < 0.0f)
            {
                fireCooldown = 0.0f;
            }
        }

        // 4) 발사 처리(자동사격: 버튼 유지 시 연사)
        if (fireHeld == true)
        {
            if(launcher != null && useBulletPrefab == true)
            {
                launcher.FireOne();
            }
            else
            {
                TryFire();
            }
        }
    }

    // ===== 입력 바인딩 =====
    public void SetFireHeld(bool held)
    {
        fireHeld = held;
    }

    public void SetAdsHeld(bool held)
    {
        adsHeld = held;
    }

    public void RequestReload()
    {
        if (isReloading == true)
        {
            return;
        }
        if (ammoInMag >= magSize)
        {
            // 이미 가득 차 있으면 무시.
            return;
        }
        if (reserveAmmo <= 0)
        {
            return;
        }
        StartCoroutine(CoReload());
    }

    // ===== 핵심 로직 =====

    private void TryFire()
    {
        // 1) 쿨다운 확인.
        if (fireCooldown > 0.0f)
        {
            return;
        }

        // 2) 탄약 확인.
        if (ammoInMag <= 0)
        {
            PlayDryFire();
            // 쿨다운을 약간 줘서 클릭 중복 억제.
            fireCooldown = 0.2f;
            return;
        }

        // 3) 1발 소비.
        --ammoInMag;
        UpdateAmmoHud();

        // 4) 발사 쿨다운 셋.
        float interval = 1.0f / fireRate;
        fireCooldown = interval;

        // 5) 이펙트/사운드/반동.
        PlayMuzzle();
        PlayShotSfx();
        ApplyRecoilKick();

        // 6) 히트스캔.
        DoHitscan();

        // 7) 크로스헤어 확산 펄스.
        if (crosshair != null)
        {
            crosshair.PulseFireSpread();
        }
    }

    private IEnumerator CoReload()
    {
        isReloading = true;

        // (선택) 재장전 사운드/애니메이션 훅.
        // audioSource.PlayOneShot(reloadSfx);

        yield return new WaitForSeconds(reloadTime);

        int needed = magSize - ammoInMag;
        if (needed < 0)
        {
            needed = 0;
        }
        int toLoad = Mathf.Min(needed, reserveAmmo);

        ammoInMag += toLoad;
        reserveAmmo -= toLoad;

        // 실탄 한 발 장전 방식이면, 빈 탄창에서 리로드 시 +1 허용.
        if (chamberedRound == true)
        {
            if (ammoInMag > 0)
            {
                // 이미 한 발 장전되어 있다고 가정 -> 규칙에 맞게 조정 가능.
            }
        }

        UpdateAmmoHud();
        isReloading = false;
    }

    private void DoHitscan()
    {
        if (playerCamera == null)
        {
            return;
        }

        // 카메라 정중앙에서 레이캐스트.
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;
        bool got = Physics.Raycast(ray, out hit, maxRange, hitMask, QueryTriggerInteraction.Ignore);

        if (got == true)
        {
            // 데미지 전달.
            float finalDamage = damage;

            Hitbox hb = hit.collider.GetComponent<Hitbox>();
            if (hb != null)
            {
                Debug.Log("name = " + hb.gameObject.name);                
                if (applyHitboxMultiplier == true)
                {
                    finalDamage = finalDamage * hb.damageMultiplier;
                }

                if (hb.owner != null)
                {
                    Debug.Log("Damage = " + finalDamage);
                    hb.owner.ApplyDamage(finalDamage, hit.point, hit.normal, transform);
                }
                else
                {
                    IDamageable id = hit.collider.GetComponentInParent<IDamageable>();
                    if (id != null)
                    {
                        Debug.Log("Damage = " + finalDamage);
                        id.ApplyDamage(finalDamage, hit.point, hit.normal, transform);
                    }
                }
            }
            else
            {
                IDamageable dmg = hit.collider.GetComponentInParent<IDamageable>();
                if (dmg != null)
                {
                    Debug.Log("Damage = " + finalDamage);
                    dmg.ApplyDamage(finalDamage, hit.point, hit.normal, transform);
                }
            }

            // 데미지 인터페이스가 있다면 TryGetComponent로 처리 가능(데모에선 VFX/데칼만)
            //SpawnHitVfx(hit.point, hit.normal);
            //SpawnDecal(hit.point, hit.normal);

            // 표면 임팩트(오디오/VFX/데칼)
            if (impactRouter != null)
            {
                impactRouter.SpawnImpact(hit);
            }
            else
            {
                // 기본 VFX/데칼 호출이 있었다면, 이제 Router로 대체하는 걸 권장
                SpawnHitVfx(hit.point, hit.normal);
                SpawnDecal(hit.point, hit.normal);
            }
        }
    }

    private void SpawnHitVfx(Vector3 point, Vector3 normal)
    {
        if (hitVfxPrefab == null)
        {
            return;
        }

        Quaternion rot = Quaternion.LookRotation(Vector3.ProjectOnPlane(normal, Vector3.up), normal);
        ParticleSystem vfx = Instantiate(hitVfxPrefab, point + normal * 0.01f, rot);
        // 자동 파괴 설정이 프리팹에 있지 않다면, 수명 후 Destroy 추가 가능.
    }

    private void SpawnDecal(Vector3 point, Vector3 normal)
    {
        if (bulletDecalPrefab == null)
        {
            return;
        }

        Quaternion rot = Quaternion.LookRotation(-normal, Vector3.up);
        GameObject decal = Instantiate(bulletDecalPrefab, point + normal * 0.002f, rot);
        // 필요하면 부모를 히트한 콜라이더로 설정하여 이동에 추적.
    }

    private void PlayMuzzle()
    {
        //if (muzzleFlash != null)
        //{
        //    muzzleFlash.Play();
        //}
        if(muzzleLight != null)
        {
            GameObject go = Instantiate(muzzleLight, muzzleTransform.position, muzzleTransform.rotation, muzzleTransform);
            if(go != null)
            {
                Destroy(go, 0.2f);
            }
        }
    }

    private void PlayShotSfx()
    {
        if (audioSource != null)
        {
            if (shotSfx != null)
            {
                audioSource.PlayOneShot(shotSfx, 1.0f);
            }
        }
    }

    private void PlayDryFire()
    {
        if (audioSource != null)
        {
            if (drySfx != null)
            {
                audioSource.PlayOneShot(drySfx, 1.0f);
            }
        }
    }

    private void ApplyRecoilKick()
    {
        if (recoil != null)
        {
            recoil.Kick();
        }
    }

    private void UpdateAdsFov(float dt)
    {
        if (playerCamera == null)
        {
            return;
        }

        float target = hipFov;
        if (adsHeld == true)
        {
            target = adsFov;
        }

        // SmoothDamp 기반 FOV 전환.
        float newFov = Mathf.SmoothDamp(playerCamera.fieldOfView, target, ref fovVel, adsBlendTime);
        playerCamera.fieldOfView = newFov;

        // (선택) 마우스 감도 배율.
        if (applyMouseSensitivityScale == true)
        {
            MouseLook ml = playerCamera.GetComponentInParent<MouseLook>();
            if (ml != null)
            {
                if (adsHeld == true)
                {
                    ml.SetSensitivityMultiplier(adsMouseScale);
                }
                else
                {
                    ml.SetSensitivityMultiplier(1.0f);
                }
            }
        }
    }

    private void UpdateAmmoHud()
    {
        if (ammoHud != null)
        {
            ammoHud.SetAmmo(ammoInMag, reserveAmmo);
        }
    }
}
