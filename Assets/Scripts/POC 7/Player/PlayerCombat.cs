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
        [SerializeField] private int _attackPowerGainPerKill = 1;

        /// <summary>공격력 1일 때 플레이어의 기본 크기.</summary>
        [SerializeField] private float _basePlayerSize = 0.3f;

        /// <summary>공격력 1 증가당 추가되는 크기. 공격력 128에서 최대 크기에 도달한다.</summary>
        [SerializeField] private float _sizePerAttackPower = 0.013f;

        /// <summary>플레이어 크기의 상한. 공격력 128 이상에서 이 값으로 고정된다.</summary>
        [SerializeField] private float _maxPlayerSize = 2.0f;

        private int _currentAttackPower;

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
        /// 돌진 출발점부터 목표 지점까지 레이를 쏴서 경로 위의 모든 IDamageable에 데미지를 적용한다.
        /// PlayerController가 StartDash 시점에 호출한다.
        ///
        /// [실무 권장]
        /// Physics2D.RaycastAll은 선(1D) 판정이라 플레이어 폭을 무시한다.
        /// 더 자연스러운 판정이 필요하면 Physics2D.CircleCastAll로 교체하여
        /// 플레이어 콜라이더 반경만큼의 두께를 줄 수 있다.
        /// </summary>
        /// <param name="from">돌진 출발 위치 (world space).</param>
        /// <param name="to">돌진 목표 위치 (world space).</param>
        public void PerformDashAttack(Vector2 from, Vector2 to)
        {
            Vector2 direction = (to - from).normalized;
            float distance = Vector2.Distance(from, to);

            // 레이 경로 위의 모든 콜라이더를 한 번에 조회한다.
            // RaycastAll은 출발점을 포함한 콜라이더도 반환할 수 있으므로 IDamageable 여부로 필터링한다.
            RaycastHit2D[] hits = Physics2D.RaycastAll(from, direction, distance);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.TryGetComponent(out IDamageable damageable))
                    damageable.TakeDamage(_currentAttackPower);
            }
        }

        /// <summary>
        /// 적이 처치될 때마다 호출된다. 공격력을 증가시키고 플레이어 크기를 갱신한다.
        /// </summary>
        private void HandleEnemyKilled(Enemy enemy)
        {
            _currentAttackPower += _attackPowerGainPerKill;
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
