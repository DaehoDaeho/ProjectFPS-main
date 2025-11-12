using UnityEngine;

/// <summary>
/// PlayerHealth.onDamaged 이벤트를 받아서 ScreenDamageOverlay를 호출하는 브리지.
/// - 인스펙터에서 PlayerHealth를 연결(같은 오브젝트에 있어도 되고, 자식/부모여도 됨)
/// </summary>
public class DamageOverlayBridge : MonoBehaviour
{
    public PlayerHealth playerHealth;    // 데미지 이벤트를 제공하는 컴포넌트
    public float assumedMaxHealth = 100.0f; // Overlay 강도 계산에 사용할 최대 체력(동기화용)

    private void Awake()
    {
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
        }

        // PlayerHealth가 없으면 동작 불가.
        if (playerHealth == null)
        {
            Debug.LogWarning("DamageOverlayBridge: PlayerHealth not found.");
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.onDamaged.AddListener(OnPlayerDamaged);
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.onDamaged.RemoveListener(OnPlayerDamaged);
        }
    }

    /// <summary>
    /// 플레이어가 데미지를 받았을 때 호출되는 콜백.
    /// 데미지 양에 비례해 오버레이를 재생한다.
    /// </summary>
    private void OnPlayerDamaged(float amount)
    {
        // 오버레이 인스턴스 확인
        if (ScreenDamageOverlay.instance == null)
        {
            return;
        }

        float maxHP = assumedMaxHealth;

        // PlayerHealth에서 실제 최대 체력을 얻을 수 있으면 사용
        if (playerHealth != null)
        {
            maxHP = playerHealth.maxHealth;
        }

        ScreenDamageOverlay.instance.PlayDamageFlash(amount, maxHP);
    }
}
