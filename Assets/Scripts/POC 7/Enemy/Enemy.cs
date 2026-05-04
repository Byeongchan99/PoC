using System;
using UnityEngine;

namespace POC7
{
    /// <summary>
    /// 적 오브젝트의 체력, 피격, 크기 변화, 사망 처리를 담당하는 컴포넌트.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class Enemy : MonoBehaviour, IDamageable
    {
        /// <summary>적이 처치될 때 발생하는 정적 이벤트. GameManager, Player 등이 구독한다.</summary>
        public static event Action<Enemy> OnEnemyKilled;

        [SerializeField] private int _maxHealth = 1;
        [SerializeField] private float _baseSize = 0.3f;
        [SerializeField] private float _sizePerHealth = 0.1f;
        [SerializeField] private float _maxVisualSize = 2.0f;

        private int _currentHealth;

        /// <summary>현재 체력이 1 이상이면 생존 상태로 판단한다.</summary>
        public bool IsAlive => _currentHealth > 0;

        /// <summary>
        /// Rigidbody2D를 Kinematic으로, CircleCollider2D를 트리거 모드로 설정한다.
        /// </summary>
        private void Awake()
        {
            var rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            var col = GetComponent<CircleCollider2D>();
            col.isTrigger = true;
        }

        /// <summary>
        /// Spawner가 적을 생성한 직후 호출한다. 체력을 설정하고 크기를 초기화한다.
        /// </summary>
        public void Initialize(int health)
        {
            _maxHealth = health;
            _currentHealth = health;
            UpdateVisualSize();
        }

        /// <summary>
        /// 지정한 양만큼 체력을 감소시킨다. 체력이 0 이하가 되면 사망 처리한다.
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (!IsAlive)
                return;

            _currentHealth -= damage;
            UpdateVisualSize();

            if (_currentHealth <= 0)
                Die();
        }

        /// <summary>
        /// 현재 체력에 비례하여 오브젝트 크기를 갱신한다. maxVisualSize를 초과하지 않도록 보정한다.
        /// </summary>
        private void UpdateVisualSize()
        {
            float size = Mathf.Min(_baseSize + _currentHealth * _sizePerHealth, _maxVisualSize);
            transform.localScale = Vector3.one * size;
        }

        /// <summary>
        /// OnEnemyKilled 이벤트를 발생시킨 후 오브젝트를 비활성화한다.
        /// 오브젝트 풀링 도입 시 Destroy 대신 비활성화 방식을 유지한다.
        /// </summary>
        private void Die()
        {
            OnEnemyKilled?.Invoke(this);
            gameObject.SetActive(false);
        }
    }
}
