using System;
using System.Collections.Generic;
using UnityEngine;

namespace POC6
{
    /// <summary>
    /// 적 AI를 담당합니다.
    /// - 우주선의 가장 가까운 노드를 향해 이동
    /// - 사거리 안에 들어오면 정지 후 원거리 공격
    /// - 처치 시 골드 드롭 이벤트 발행
    /// 스탯은 EnemyData ScriptableObject에서 주입받습니다.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        /// <summary>씬에 활성화된 모든 Enemy 목록. AttackNodeBehaviour가 타겟 탐색에 사용합니다.</summary>
        public static readonly List<Enemy> AllActive = new();

        /// <summary>적이 처치되었을 때 발행됩니다. (드롭 골드 양)</summary>
        public static event Action<int> OnGoldDropped;

        /// <summary>적이 처치되었을 때 발행됩니다. (이 적 인스턴스)</summary>
        public static event Action<Enemy> OnEnemyDied;

        [Header("디버그 (읽기 전용)")]
        [SerializeField] private float _currentHealth;

        // 이 적의 설정 데이터 (EnemySpawner가 주입)
        private EnemyData _data;

        // 우주선 그리드 참조 (가장 가까운 노드 탐색용)
        private ShipGrid _shipGrid;

        // 목표 노드를 갱신하는 주기 타이머
        private float _targetUpdateTimer = 0f;
        private const float TARGET_UPDATE_INTERVAL = 0.15f;

        // 현재 추적 중인 노드
        private PlacedNode _currentTarget;

        // 마지막 공격 시간
        private float _lastAttackTime = 0f;

        private bool _isDead = false;

        private Rigidbody2D _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            if (_rigidbody != null)
                _rigidbody.gravityScale = 0f;
        }

        private void OnEnable() => AllActive.Add(this);

        private void OnDisable() => AllActive.Remove(this);

        private void Update()
        {
            if (_isDead) return;

            // 목표 노드를 주기적으로 갱신 (매 프레임보다 저렴)
            _targetUpdateTimer -= Time.deltaTime;
            if (_targetUpdateTimer <= 0f)
            {
                UpdateTargetNode();
                _targetUpdateTimer = TARGET_UPDATE_INTERVAL;
            }

            // 목표가 없으면 대기
            if (_currentTarget == null) return;

            Vector3 targetWorldPos = _shipGrid.NodeCenterToWorld(_currentTarget);
            float distToTarget = Vector3.Distance(transform.position, targetWorldPos);

            if (distToTarget <= _data.AttackRange)
            {
                // 사거리 안 - 정지 후 공격
                StopMovement();
                TryAttack(targetWorldPos);
            }
            else
            {
                // 사거리 밖 - 목표 방향으로 이동
                MoveToward(targetWorldPos);
            }
        }

        // ────────────────────────────────────────────────
        // 공개 API (EnemySpawner에서 호출)
        // ────────────────────────────────────────────────

        /// <summary>
        /// 적을 초기화합니다. 스폰 시 EnemySpawner에서 호출합니다.
        /// </summary>
        public void Initialize(EnemyData data, ShipGrid grid)
        {
            _data = data;
            _shipGrid = grid;
            _currentHealth = data.MaxHealth;
            _isDead = false;
            _lastAttackTime = 0f;
            _currentTarget = null;
        }

        /// <summary>
        /// 데미지를 받습니다. Projectile의 OnTriggerEnter2D에서 호출됩니다.
        /// </summary>
        public void TakeDamage(float amount)
        {
            if (_isDead) return;

            _currentHealth -= amount;

            if (_currentHealth <= 0f)
                Die();
        }

        // ────────────────────────────────────────────────
        // 내부 AI 로직
        // ────────────────────────────────────────────────

        /// <summary>
        /// 우주선 노드 중에서 자신과 가장 가까운 노드를 찾아 현재 목표로 설정합니다.
        /// 매 프레임 대신 일정 간격으로 호출해서 성능을 절약합니다.
        /// </summary>
        private void UpdateTargetNode()
        {
            if (_shipGrid == null) return;

            PlacedNode closest = null;
            float closestDist = float.MaxValue;

            foreach (var node in _shipGrid.PlacedNodes)
            {
                float dist = Vector3.Distance(transform.position, _shipGrid.NodeCenterToWorld(node));
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = node;
                }
            }

            _currentTarget = closest;
        }

        /// <summary>
        /// 대상 방향으로 이동합니다.
        /// </summary>
        private void MoveToward(Vector3 targetPos)
        {
            Vector2 direction = ((Vector2)targetPos - (Vector2)transform.position).normalized;

            if (_rigidbody != null)
                _rigidbody.linearVelocity = direction * _data.MoveSpeed;
            else
                transform.position += (Vector3)direction * _data.MoveSpeed * Time.deltaTime;
        }

        /// <summary>
        /// 이동을 정지합니다.
        /// </summary>
        private void StopMovement()
        {
            if (_rigidbody != null)
                _rigidbody.linearVelocity = Vector2.zero;
        }

        /// <summary>
        /// 공격 간격이 충족되면 우주선을 향해 발사합니다.
        /// </summary>
        private void TryAttack(Vector3 targetPos)
        {
            if (Time.time - _lastAttackTime < _data.AttackInterval) return;

            Vector2 direction = ((Vector2)targetPos - (Vector2)transform.position).normalized;

            ProjectilePool.Instance?.Get(
                transform.position,
                direction,
                _data.ProjectileSpeed,
                _data.AttackDamage,
                _data.AttackRange * 1.5f,
                0,  // 적 발사체는 관통 없음
                "Enemy"
            );

            _lastAttackTime = Time.time;
        }

        /// <summary>
        /// 처치 처리: 이벤트 발행 후 비활성화합니다.
        /// </summary>
        private void Die()
        {
            _isDead = true;
            StopMovement();

            OnGoldDropped?.Invoke(_data.GoldDropAmount);
            OnEnemyDied?.Invoke(this);

            gameObject.SetActive(false);
        }
    }
}
