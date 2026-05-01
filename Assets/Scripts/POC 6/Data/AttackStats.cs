using System;
using UnityEngine;

namespace POC6
{
    /// <summary>
    /// 공격 노드의 기본 전투 스탯을 담는 구조체입니다.
    /// ScriptableObject 필드로 사용되기 때문에 직렬화 가능하게 만들었습니다.
    /// </summary>
    [Serializable]
    public struct AttackStats
    {
        [Header("발사체 설정")]
        [Tooltip("발사체 하나의 데미지")]
        [SerializeField] private float _damage;

        [Tooltip("초당 발사 횟수")]
        [SerializeField] private float _fireRate;

        [Tooltip("자동 조준 사거리")]
        [SerializeField] private float _attackRange;

        [Tooltip("발사체 이동 속도")]
        [SerializeField] private float _projectileSpeed;

        [Header("멀티샷 설정")]
        [Tooltip("기본 발사체 개수 (멀티샷 특수 효과로 증가 가능)")]
        [SerializeField] private int _projectileCount;

        [Header("관통 설정")]
        [Tooltip("발사체가 관통할 수 있는 최대 적 수 (0이면 관통 없음)")]
        [SerializeField] private int _pierceCount;

        // 외부에서 읽기 전용으로 접근할 수 있는 프로퍼티들
        public float Damage => _damage;
        public float FireRate => _fireRate;
        public float AttackRange => _attackRange;
        public float ProjectileSpeed => _projectileSpeed;
        public int ProjectileCount => _projectileCount;
        public int PierceCount => _pierceCount;

        /// <summary>
        /// 기본값을 직접 지정해서 AttackStats를 생성합니다.
        /// </summary>
        public AttackStats(float damage, float fireRate, float attackRange, float projectileSpeed, int projectileCount = 1, int pierceCount = 0)
        {
            _damage = damage;
            _fireRate = fireRate;
            _attackRange = attackRange;
            _projectileSpeed = projectileSpeed;
            _projectileCount = projectileCount;
            _pierceCount = pierceCount;
        }

        /// <summary>
        /// 동력과 특수 효과를 반영하여 실제 전투에서 사용할 스탯을 계산해서 반환합니다.
        /// powerRatio: 받은 동력 / 최대 동력 (0~1 범위)
        /// </summary>
        public AttackStats WithPowerAndEffects(float powerRatio, SpecialEffectType? specialEffect, float effectMagnitude)
        {
            // 동력 비율에 따라 공격력과 공격속도 스케일 (POC 기준 단순 선형 스케일)
            float scaledDamage = _damage * powerRatio;
            float scaledFireRate = _fireRate * powerRatio;

            int finalProjectileCount = _projectileCount;
            int finalPierceCount = _pierceCount;

            // 특수 효과 적용
            if (specialEffect.HasValue)
            {
                switch (specialEffect.Value)
                {
                    case SpecialEffectType.Multishot:
                        // effectMagnitude를 추가 발사체 수로 사용
                        finalProjectileCount += Mathf.RoundToInt(effectMagnitude);
                        break;
                    case SpecialEffectType.Pierce:
                        // effectMagnitude를 추가 관통 수로 사용
                        finalPierceCount += Mathf.RoundToInt(effectMagnitude);
                        break;
                }
            }

            return new AttackStats(scaledDamage, scaledFireRate, _attackRange, _projectileSpeed, finalProjectileCount, finalPierceCount);
        }
    }
}
