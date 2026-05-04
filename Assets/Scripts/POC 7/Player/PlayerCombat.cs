using UnityEngine;

namespace POC7
{
    /// <summary>
    /// 플레이어의 공격력 관리와 돌진 중 적 충돌 피해를 담당하는 컴포넌트.
    /// PlayerController와 같은 GameObject에 부착해야 한다.
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private int _initialAttackPower = 1;
        [SerializeField] private int _attackPowerGainPerKill = 1;

        private PlayerController _playerController;
        private int _currentAttackPower;

        /// <summary>현재 공격력. 외부에서 읽기 전용으로 참조한다.</summary>
        public int CurrentAttackPower => _currentAttackPower;

        /// <summary>
        /// 컴포넌트 초기화. PlayerController 참조를 캐시하고 공격력을 초기값으로 설정한다.
        /// </summary>
        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            _currentAttackPower = _initialAttackPower;
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
        /// 씬 전환이나 오브젝트 비활성화 시 메모리 누수를 방지한다.
        /// </summary>
        private void OnDisable()
        {
            Enemy.OnEnemyKilled -= HandleEnemyKilled;
        }

        /// <summary>
        /// 돌진 중 트리거 영역에 진입한 Collider2D를 IDamageable로 캐스팅하여 데미지를 적용한다.
        /// Dashing 상태가 아니면 아무 처리도 하지 않는다.
        /// </summary>
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!_playerController.IsDashing)
                return;

            if (other.TryGetComponent(out IDamageable damageable))
                damageable.TakeDamage(_currentAttackPower);
        }

        /// <summary>
        /// 적이 처치될 때마다 호출된다. 공격력을 attackPowerGainPerKill만큼 증가시킨다.
        /// </summary>
        private void HandleEnemyKilled(Enemy enemy)
        {
            _currentAttackPower += _attackPowerGainPerKill;
        }
    }
}
