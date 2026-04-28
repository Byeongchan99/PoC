using UnityEngine;

namespace POC4
{
    /// <summary>
    /// 포탄 타워: 타겟을 향해 투사체를 발사하고 착탄 시 범위 피해를 주는 타워.
    /// ArrowTower와 공격 구조가 같지만 CannonProjectile을 사용해 AoE 피해를 처리한다.
    /// Tower 기본 AttackLoop를 그대로 사용하므로 Attack() 메서드만 구현한다.
    /// </summary>
    public class CannonTower : Tower
    {
        // -------------------------------------------------------
        // Inspector 노출 필드
        // -------------------------------------------------------

        [Header("Cannon Tower Settings")]
        [Tooltip("발사할 포탄 프리팹 (CannonProjectile 컴포넌트 필수)")]
        [SerializeField] private CannonProjectile _projectilePrefab;

        [Tooltip("포탄 이동 속도 (월드 단위/초)")]
        [SerializeField] private float _projectileSpeed = 5f;

        [Tooltip("폭발 반경 (월드 단위). 이 범위 안의 모든 적이 동일한 피해를 받는다.")]
        [SerializeField] private float _explosionRadius = 1.5f;

        // -------------------------------------------------------
        // 공격 구현
        // -------------------------------------------------------

        /// <summary>
        /// 타겟 방향으로 포탄 투사체를 생성하고 폭발 반경을 전달한다.
        /// Tower의 AttackLoop()에서 주기적으로 호출된다.
        /// </summary>
        protected override void Attack(Enemy target)
        {
            if (_projectilePrefab == null)
            {
                Debug.LogWarning("[CannonTower] CannonProjectile 프리팹이 연결되지 않았습니다.");
                return;
            }

            CannonProjectile projectile = Instantiate(_projectilePrefab, transform.position, Quaternion.identity);
            projectile.Initialize(target, _attackPower, _projectileSpeed, _explosionRadius,
                                  _effectType, _extraDamage, _slowRatio, _slowDuration, _stunDuration);
        }

        // -------------------------------------------------------
        // Scene 뷰 Gizmo (폭발 반경 표시)
        // -------------------------------------------------------

        /// <summary>
        /// Scene 뷰에서 타워 사거리(부모 Gizmo)와 함께 폭발 반경을 노란 원으로 표시한다.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, _explosionRadius);
        }
    }
}
