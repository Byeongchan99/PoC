using System;
using UnityEngine;

namespace POC7
{
    /// <summary>
    /// 플레이어의 공격력 관리, 돌진 경로 레이캐스트 공격, 크기 변화를 담당하는 컴포넌트.
    /// PlayerController와 같은 GameObject에 부착해야 한다.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        /// <summary>공격력이 변경될 때 발생. 인자는 변경 후 공격력. PlayerAttackLabel이 구독한다.</summary>
        public event Action<int> OnAttackPowerChanged;

        [SerializeField] private int _initialAttackPower = 1;

        /// <summary>공격력 1일 때 플레이어의 기본 크기.</summary>
        [SerializeField] private float _basePlayerSize = 0.3f;

        /// <summary>공격력 1 증가당 추가되는 크기. 공격력 128에서 최대 크기에 도달한다.</summary>
        [SerializeField] private float _sizePerAttackPower = 0.013f;

        /// <summary>플레이어 크기의 상한. 공격력 128 이상에서 이 값으로 고정된다.</summary>
        [SerializeField] private float _maxPlayerSize = 2.0f;

        private int _currentAttackPower;

        /// <summary>
        /// 현재 공격력만큼 적을 처치하면 공격력이 2배로 증가한다.
        /// 증가 후에도 킬 카운트는 유지되어, 다음 배수까지 필요한 킬 수가 줄어든다.
        /// 예: 공격력 16 달성 시 킬 카운트 16 → 공격력 32, 킬 카운트 16 유지 → 이후 16킬 추가하면 64
        /// </summary>
        private int _killCount;

        /// <summary>현재 공격력. 외부에서 읽기 전용으로 참조한다.</summary>
        public int CurrentAttackPower => _currentAttackPower;

        /// <summary>
        /// 공격력을 초기값으로 설정하고 초기 크기를 반영한다.
        /// </summary>
        private void Awake()
        {
            _currentAttackPower = _initialAttackPower;
            UpdatePlayerSize();
        }

        /// <summary>
        /// 오브젝트 활성화 시 Enemy 처치 이벤트를 구독한다.
        /// </summary>
        private void OnEnable()
        {
            Enemy.OnEnemyKilled += HandleEnemyKilled;
        }

        /// <summary>
        /// 오브젝트 비활성화 시 Enemy 처치 이벤트 구독을 해제한다.
        /// </summary>
        private void OnDisable()
        {
            Enemy.OnEnemyKilled -= HandleEnemyKilled;
        }

        /// <summary>
        /// 돌진 출발점부터 목표 지점까지 원형 캐스트를 쏴서 경로 위의 모든 IDamageable에 데미지를 적용한다.
        /// PlayerController가 StartDash 시점에 호출한다.
        ///
        /// [실무 권장]
        /// CircleCastAll은 플레이어 반경만큼의 두께로 판정하여 RaycastAll보다 자연스럽다.
        /// 더 정밀한 표현이 필요하면 LayerMask를 지정하여 불필요한 레이어를 제외할 수 있다.
        /// </summary>
        /// <param name="from">돌진 출발 위치 (world space).</param>
        /// <param name="to">돌진 목표 위치 (world space).</param>
        public void PerformDashAttack(Vector2 from, Vector2 to)
        {
            Vector2 direction = (to - from).normalized;
            float distance = Vector2.Distance(from, to);

            // 플레이어 localScale.x가 지름이므로 반으로 나눠 반경을 구한다.
            // 크기가 커져도 자동으로 판정 두께가 반영된다.
            float radius = transform.localScale.x / 2f;

            RaycastHit2D[] hits = Physics2D.CircleCastAll(from, radius, direction, distance);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.TryGetComponent(out IDamageable damageable))
                    damageable.TakeDamage(_currentAttackPower);
            }
        }

        /// <summary>
        /// 적이 처치될 때마다 킬 카운트를 증가시킨다.
        /// 킬 카운트가 현재 공격력에 도달하면 공격력을 2배로 올리고, 킬 카운트는 유지한다.
        /// </summary>
        private void HandleEnemyKilled(Enemy enemy)
        {
            _killCount++;

            if (_killCount < _currentAttackPower)
                return;

            _currentAttackPower *= 2;
            UpdatePlayerSize();
            OnAttackPowerChanged?.Invoke(_currentAttackPower);
        }

        /// <summary>
        /// 현재 공격력에 비례하여 플레이어 크기를 갱신한다.
        /// 크기 공식: basePlayerSize + attackPower * sizePerAttackPower (상한: maxPlayerSize)
        /// 공격력 128 기준: 0.3 + 128 * 0.013 = 약 1.96 → maxPlayerSize(2.0)로 고정
        /// </summary>
        private void UpdatePlayerSize()
        {
            float size = Mathf.Min(_basePlayerSize + _currentAttackPower * _sizePerAttackPower, _maxPlayerSize);
            transform.localScale = Vector3.one * size;
        }
    }
}
