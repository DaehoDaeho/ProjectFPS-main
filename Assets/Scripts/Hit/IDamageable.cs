using UnityEngine;

/// <summary>
/// 모든 피격 가능 객체가 구현할 공용 인터페이스.
/// 무기는 이 인터페이스만 보고 데미지를 전달.
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// 데미지를 적용.
    /// </summary>
    /// <param name="amount">적용할 데미지</param>
    /// <param name="hitPoint">맞은 월드 좌표</param>
    /// <param name="hitNormal">맞은 표면 법선</param>
    /// <param name="source">공격자 트랜스폼</param>
    void ApplyDamage(float amount, Vector3 hitPoint, Vector3 hitNormal, Transform source);
}
