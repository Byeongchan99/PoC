using System.Collections;
using UnityEngine;

namespace POC4
{
    /// <summary>
    /// 모든 타워의 기본 클래스.
    /// 스탯 초기화, 사거리 내 타겟 선택, Coroutine 기반 공격 루프를 담당한다.
    /// 실제 공격 방식은 하위 클래스(ArrowTower, LaserTower, CannonTower)에서 구현한다.
    /// </summary>
    public abstract class Tower : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector 노출 필드 (디버그 및 런타임 수치 확인용)
        // -------------------------------------------------------

        [Header("Stats (Initialize()로 설정됨 - 디버그 확인용)")]
        [SerializeField] protected float _attackPower = 10f;
        [SerializeField] protected float _range = 3f;
        [SerializeField] protected float _attackSpeed = 1f;

        [Header("Effect")]
        [SerializeField] protected TowerData.TowerEffectType _effectType = TowerData.TowerEffectType.None;

        // 효과 수치 (6단계에서 적용)
        [SerializeField] protected float _extraDamage = 5f;
        [SerializeField] protected float _slowRatio = 0.5f;
        [SerializeField] protected float _slowDuration = 2f;
        [SerializeField] protected float _stunDuration = 1f;

        // -------------------------------------------------------
        // 내부 상태
        // -------------------------------------------------------

        private Coroutine _attackCoroutine;

        // -------------------------------------------------------
        // 초기화
        // -------------------------------------------------------

        /// <summary>
        /// TowerData를 기반으로 스탯을 설정하고 공격 루프를 시작한다.
        /// TowerPlacer가 Instantiate 직후에 호출한다.
        /// </summary>
        public void Initialize(TowerData data)
        {
            _attackPower = data.AttackPower;
            _range = data.Range;
            _attackSpeed = data.AttackSpeed;
            _effectType = data.EffectType;
            _extraDamage = data.ExtraDamage;
            _slowRatio = data.SlowRatio;
            _slowDuration = data.SlowDuration;
            _stunDuration = data.StunDuration;

            _attackCoroutine = StartCoroutine(AttackLoop());
        }

        /// <summary>
        /// 벽 효과를 타워 스탯에 추가 적용한다.
        /// TowerPlacer가 Initialize() 이후에 호출한다. (6단계에서 본격 연동)
        /// </summary>
        public void ApplyWallBonus(WallData wallData)
        {
            switch (wallData.EffectType)
            {
                case WallData.WallEffectType.AttackBoost:
                    _attackPower += wallData.AttackBonus;
                    break;
                case WallData.WallEffectType.RangeBoost:
                    _range += wallData.RangeBonus;
                    break;
                case WallData.WallEffectType.AttackSpeedBoost:
                    _attackSpeed += wallData.AttackSpeedBonus;
                    break;
            }
        }

        // -------------------------------------------------------
        // 공격 루프
        // -------------------------------------------------------

        /// <summary>
        /// 일정 간격으로 타겟을 찾아 공격하는 Coroutine.
        /// 공격 간격 = 1 / 공격속도(초).
        /// </summary>
        private IEnumerator AttackLoop()
        {
            while (true)
            {
                // 공격 간격 대기 후 타겟 탐색 및 공격
                yield return new WaitForSeconds(1f / _attackSpeed);

                Enemy target = FindFurthestEnemyInRange();
                if (target != null && target.IsAlive)
                {
                    Attack(target);
                }
            }
        }

        /// <summary>
        /// 타워 종류별 공격 로직. 하위 클래스에서 반드시 구현해야 한다.
        /// </summary>
        protected abstract void Attack(Enemy target);

        // -------------------------------------------------------
        // 타겟 선택
        // -------------------------------------------------------

        /// <summary>
        /// 사거리 내 모든 적 중 PathProgress가 가장 높은 적을 반환한다.
        /// PathProgress가 높을수록 목표 지점에 가까운 (= 가장 위험한) 적이다.
        /// 사거리 내 적이 없으면 null 반환.
        /// </summary>
        private Enemy FindFurthestEnemyInRange()
        {
            // FindObjectsByType은 Destroy 예약된 오브젝트를 포함하지 않음
            Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

            Enemy bestTarget = null;
            float highestProgress = -1f;

            foreach (Enemy enemy in allEnemies)
            {
                if (!enemy.IsAlive) continue;

                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance <= _range && enemy.PathProgress > highestProgress)
                {
                    highestProgress = enemy.PathProgress;
                    bestTarget = enemy;
                }
            }

            return bestTarget;
        }

        // -------------------------------------------------------
        // 정리
        // -------------------------------------------------------

        private void OnDestroy()
        {
            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
            }
        }

        // -------------------------------------------------------
        // Scene 뷰 Gizmo
        // -------------------------------------------------------

        /// <summary>
        /// Scene 뷰에서 타워 사거리를 원으로 표시한다.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (_range <= 0f) return;
            Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, _range);
        }
    }
}
