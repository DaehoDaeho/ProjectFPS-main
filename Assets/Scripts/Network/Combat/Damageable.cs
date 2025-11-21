using UnityEngine;

/// <summary>
/// 서버 권한으로 HP를 관리하는 간단한 Damageable.
/// - HP 감소/사망 이벤트만 제공(네트워크 동기화는 서버 STATE로 해결).
/// - 서버에서만 TakeDamage를 호출한다고 가정.
/// </summary>
public class Damageable : MonoBehaviour
{
    [Header("Stats")]
    public int maxHp = 100;            // 최대 체력
    public int currentHp = 100;        // 현재 체력

    public void ResetHp()
    {
        currentHp = maxHp;
    }

    public void TakeDamage(int amount)
    {
        // amount가 0 이하이면 무시
        if (amount <= 0)
        {
            return;
        }

        currentHp = currentHp - amount;
        if (currentHp < 0)
        {
            currentHp = 0;
        }

        if (currentHp == 0)
        {
            OnDeath();
        }
    }

    private void OnDeath()
    {
        // 여기서는 서버가 리스폰을 담당하므로 로컬에선 로직 없음
        // 사운드/이펙트는 클라에서 STATE를 보고 별도 연출 가능
    }
}
