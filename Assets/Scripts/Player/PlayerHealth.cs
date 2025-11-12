using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 플레이어용 체력 구현.
/// 플레이어도 IDamageable을 구현해야 공격이 들어온다..
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    public float maxHealth = 100.0f;     // 최대 체력.
    public UnityEvent onDeath;           // 사망 이벤트(리트라이/리스폰 연동 가능)

    public UnityEvent<float> onDamaged;         // 입은 데미지량을 전달.

    private float currentHealth;         // 현재 체력.

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitNormal, Transform source)
    {
        // 음수 방어
        float dmg = amount;
        if (dmg < 0.0f)
        {
            dmg = 0.0f;
        }

        currentHealth -= dmg;

        // 피격 이벤트 발생.
        if (onDamaged != null)
        {
            onDamaged.Invoke(dmg);
        }

        if (currentHealth <= 0.0f)
        {
            if (onDeath != null)
            {
                onDeath.Invoke();
            }
            Debug.Log("Player Dead!!!");
        }
    }

    public float GetCurrentHealth()
    {
        float v = currentHealth;
        return v;
    }

    /// <summary>
    /// 현재 체력을 비율(0~1)로 반환.
    /// </summary>
    public float GetHealthRatio01()
    {
        if (maxHealth <= 0.0f)
        {
            return 0.0f;
        }

        float r = currentHealth / maxHealth;
        if (r < 0.0f)
        {
            r = 0.0f;
        }

        if (r > 1.0f)
        {
            r = 1.0f;
        }

        return r;
    }
}
