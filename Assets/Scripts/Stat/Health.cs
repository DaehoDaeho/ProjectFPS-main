using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 기본 체력/사망 로직. IDamageable 구현체.
/// </summary>
public class Health : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 1000.0f;             // 최대 체력.
    public bool destroyOnDeath = true;           // 사망 시 오브젝트 파괴할지 여부.
    public UnityEvent onDeath;                   // 사망 이벤트(파티클/드랍 등)

    [Header("Optional Feedback")]
    public ParticleSystem hitVfxPrefab;          // 피격시 피/먼지 등.
    public AudioSource audioSource;              // 피격 사운드(선택)
    public AudioClip hurtClip;                   // 피격 효과음(선택)

    private float currentHealth;                 // 현재 체력.

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitNormal, Transform source)
    {
        if(currentHealth <= 0.0f)
        {
            return;
        }

        float dmg = amount;
        if (dmg < 0.0f)
        {
            dmg = 0.0f;
        }

        currentHealth = currentHealth - dmg;

        // 피격 피드백.
        if (hitVfxPrefab != null)
        {
            Quaternion rot = Quaternion.LookRotation(Vector3.ProjectOnPlane(hitNormal, Vector3.up), hitNormal);
            ParticleSystem vfx = Instantiate(hitVfxPrefab, hitPoint + hitNormal * 0.01f, rot);
        }
        if (audioSource != null)
        {
            if (hurtClip != null)
            {
                audioSource.PlayOneShot(hurtClip, 1.0f);
            }
        }

        if (currentHealth <= 0.0f)
        {
            if (onDeath != null)
            {
                onDeath.Invoke();
            }

            if (destroyOnDeath == true)
            {
                Destroy(gameObject);
            }
        }
    }

    public float GetCurrentHealth()
    {
        return currentHealth;
    }
}
