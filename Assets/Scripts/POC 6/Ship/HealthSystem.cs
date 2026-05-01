using System;
using UnityEngine;

namespace POC6
{
    /// <summary>
    /// 우주선의 단일 체력 풀을 관리합니다.
    /// 최대 체력 = 모든 배치 노드의 healthContribution 합계입니다.
    /// 체력이 0 이하가 되면 OnDied 이벤트를 발행합니다.
    /// </summary>
    public class HealthSystem : MonoBehaviour
    {
        /// <summary>체력이 변경될 때 발행됩니다. (현재 체력, 최대 체력)</summary>
        public static event Action<float, float> OnHealthChanged;

        /// <summary>체력이 0 이하로 떨어졌을 때 발행됩니다.</summary>
        public static event Action OnDied;

        [Header("디버그 (읽기 전용)")]
        [SerializeField] private float _currentHealth;
        [SerializeField] private float _maxHealth;

        private bool _isDead = false;

        // 읽기 전용 프로퍼티
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public float HealthRatio => _maxHealth > 0 ? _currentHealth / _maxHealth : 0f;

        /// <summary>
        /// ShipGrid의 노드 체력 합산으로 최대 체력을 초기화합니다.
        /// 웨이브 시작 전, 스냅샷 복원 후 GameManager에서 호출합니다.
        /// </summary>
        public void Initialize(ShipGrid grid)
        {
            _maxHealth = grid.CalculateTotalHealth();
            _currentHealth = _maxHealth;
            _isDead = false;

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        /// <summary>
        /// 데미지를 적용합니다. 0 이하로 떨어지면 OnDied 이벤트를 발행합니다.
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (_isDead) return;

            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);

            if (_currentHealth <= 0f)
            {
                _isDead = true;
                OnDied?.Invoke();
            }
        }

        /// <summary>
        /// 체력을 회복합니다. 최대 체력을 초과하지 않습니다.
        /// </summary>
        public void Heal(float amount)
        {
            if (_isDead) return;

            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }
    }
}
