using System;
using System.Collections.Generic;
using UnityEngine;

namespace POC6
{
    /// <summary>
    /// 개별 노드의 체력을 관리합니다.
    /// NodeVisualFactory가 노드 생성 시 자동으로 부착하며,
    /// NodeData.HealthContribution 값을 최대 체력으로 사용합니다.
    /// </summary>
    public class NodeHealth : MonoBehaviour
    {
        /// <summary>씬에 존재하는 모든 NodeHealth 인스턴스 목록. HealthSystem이 합산에 사용합니다.</summary>
        public static readonly List<NodeHealth> All = new();

        /// <summary>
        /// 이 노드가 데미지를 받았을 때 발행됩니다. (데미지 양)
        /// HealthSystem이 구독해서 우주선 전체 체력을 감소시킵니다.
        /// </summary>
        public static event Action<float> OnDamageTaken;

        [Header("디버그 (읽기 전용)")]
        [SerializeField] private float _currentHealth;
        [SerializeField] private float _maxHealth;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;

        private void OnEnable() => All.Add(this);

        private void OnDisable() => All.Remove(this);

        /// <summary>
        /// 노드 생성 시 NodeVisualFactory에서 호출합니다.
        /// NodeData.HealthContribution 값으로 최대/현재 체력을 초기화합니다.
        /// </summary>
        public void Initialize(NodeData data)
        {
            _maxHealth = data.HealthContribution;
            _currentHealth = _maxHealth;
        }

        /// <summary>
        /// 웨이브 시작 시 HealthSystem.Initialize()에서 호출합니다.
        /// 현재 체력을 최대 체력으로 되돌립니다.
        /// </summary>
        public void ResetToFull()
        {
            _currentHealth = _maxHealth;
        }

        /// <summary>
        /// 데미지를 받습니다. Projectile의 OnTriggerEnter2D에서 호출됩니다.
        /// 데미지 이벤트를 발행해 HealthSystem이 우주선 체력을 감소시키도록 합니다.
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (amount <= 0f) return;

            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            OnDamageTaken?.Invoke(amount);
        }
    }
}
