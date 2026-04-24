using UnityEngine;

namespace POC4
{
    /// <summary>
    /// 화살 타워: 단일 타겟에 투사체를 발사하는 타워.
    /// Tower 기본 클래스의 Attack()을 구현하여 Projectile을 생성한다.
    /// </summary>
    public class ArrowTower : Tower
    {
        // -------------------------------------------------------
        // Inspector 노출 필드
        // -------------------------------------------------------

        [Header("Arrow Tower Settings")]
        [Tooltip("발사할 투사체 프리팹 (Projectile 컴포넌트 필수)")]
        [SerializeField] private Projectile _projectilePrefab;

        [Tooltip("투사체 이동 속도 (월드 단위/초)")]
        [SerializeField] private float _projectileSpeed = 6f;

        // -------------------------------------------------------
        // 공격 구현
        // -------------------------------------------------------

        /// <summary>
        /// 타겟 방향으로 투사체를 생성하고 초기화한다.
        /// Tower의 AttackLoop()에서 주기적으로 호출된다.
        /// </summary>
        protected override void Attack(Enemy target)
        {
            if (_projectilePrefab == null)
            {
                Debug.LogWarning("[ArrowTower] Projectile 프리팹이 연결되지 않았습니다.");
                return;
            }

            // 투사체를 타워 위치에서 생성
            Projectile projectile = Instantiate(_projectilePrefab, transform.position, Quaternion.identity);
            projectile.Initialize(target, _attackPower, _projectileSpeed, _effectType,
                                  _extraDamage, _slowRatio, _slowDuration, _stunDuration);
        }
    }
}
