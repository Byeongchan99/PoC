using System;
using UnityEngine;

namespace POC6
{
    /// <summary>
    /// 우주선의 전체 체력 풀을 관리합니다.
    /// 개별 노드의 NodeHealth에서 데미지 이벤트를 받아 전체 체력을 감소시킵니다.
    /// 전체 체력 = 웨이브 시작 시 모든 NodeHealth.MaxHealth 합산.
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

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public float HealthRatio => _maxHealth > 0 ? _currentHealth / _maxHealth : 0f;

        private void OnEnable()
        {
            NodeHealth.OnDamageTaken += HandleNodeDamage;
        }

        private void OnDisable()
        {
            NodeHealth.OnDamageTaken -= HandleNodeDamage;
        }

        /// <summary>
        /// 웨이브 시작 시 GameManager에서 호출합니다.
        /// 모든 NodeHealth를 최대 체력으로 초기화하고 전체 최대 체력을 합산합니다.
        /// </summary>
        public void Initialize()
        {
            _isDead = false;
            _maxHealth = 0f;

            foreach (var nodeHealth in NodeHealth.All)
            {
                nodeHealth.ResetToFull();
                _maxHealth += nodeHealth.MaxHealth;
            }

            _currentHealth = _maxHealth;
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        // ────────────────────────────────────────────────
        // 내부 처리
        // ────────────────────────────────────────────────

        /// <summary>
        /// NodeHealth.OnDamageTaken 이벤트 수신 시 호출됩니다.
        /// 전체 체력에서 해당 데미지만큼 빼기만 하면 되어 재합산이 필요 없습니다.
        /// </summary>
        private void HandleNodeDamage(float amount)
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
    }
}
