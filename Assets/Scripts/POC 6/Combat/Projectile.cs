using System;
using UnityEngine;

namespace POC6
{
    /// <summary>
    /// 공격 노드와 적 모두가 사용하는 발사체입니다.
    /// Object Pool로 관리되어 성능을 최적화합니다.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [Header("비주얼 (POC: 기본 도형)")]
        [SerializeField] private SpriteRenderer _spriteRenderer;

        // 발사체 이동 속도
        private float _speed;

        // 데미지 양
        private float _damage;

        // 관통 횟수 (0이면 첫 히트 후 파괴)
        private int _pierceCount;

        // 현재까지 관통한 수
        private int _currentPierceCount;

        // 이동 방향 (월드 좌표 기준 단위 벡터)
        private Vector2 _direction;

        // 발사자 태그 (아군 발사체가 아군을 다치게 하지 않도록 구분)
        private string _ownerTag;

        // 최대 비행 거리 (이 거리를 넘으면 자동 반환)
        private float _maxRange;

        // 발사 시작 위치
        private Vector3 _startPosition;

        // 이 발사체가 반환되어야 할 때 호출되는 콜백 (Object Pool의 Return 메서드)
        private Action<Projectile> _onReturn;

        private Rigidbody2D _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            if (_rigidbody != null)
            {
                _rigidbody.gravityScale = 0f;
                _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
        }

        private void Update()
        {
            // 최대 거리를 초과하면 Pool로 반환
            if (Vector3.Distance(transform.position, _startPosition) >= _maxRange)
                ReturnToPool();
        }

        private void FixedUpdate()
        {
            // Rigidbody2D를 사용해서 물리 기반 이동
            if (_rigidbody != null)
                _rigidbody.linearVelocity = _direction * _speed;
        }

        // ────────────────────────────────────────────────
        // 공개 API (ProjectilePool에서 초기화할 때 호출)
        // ────────────────────────────────────────────────

        /// <summary>
        /// 발사체를 초기화하고 활성화합니다.
        /// Pool에서 꺼낼 때마다 이 메서드로 새 파라미터를 주입합니다.
        /// </summary>
        public void Launch(Vector3 position, Vector2 direction, float speed, float damage,
            float maxRange, int pierceCount, string ownerTag, Action<Projectile> onReturn)
        {
            transform.position = position;
            _direction = direction.normalized;
            _speed = speed;
            _damage = damage;
            _maxRange = maxRange;
            _pierceCount = pierceCount;
            _currentPierceCount = 0;
            _ownerTag = ownerTag;
            _onReturn = onReturn;
            _startPosition = position;

            gameObject.SetActive(true);
        }

        // ────────────────────────────────────────────────
        // 충돌 처리
        // ────────────────────────────────────────────────

        private void OnTriggerEnter2D(Collider2D other)
        {
            // 같은 팀은 무시
            if (other.CompareTag(_ownerTag)) return;

            // 적에게 데미지 적용
            if (_ownerTag == "Player" && other.TryGetComponent<Enemy>(out var enemy))
            {
                enemy.TakeDamage(_damage);
                HandlePierce();
            }
            // 우주선 노드에 데미지 적용
            // 각 노드의 WorldInstance에 NodeHealth가 붙어 있으므로 직접 접근
            else if (_ownerTag == "Enemy")
            {
                NodeHealth nodeHealth = other.GetComponent<NodeHealth>();
                if (nodeHealth != null)
                {
                    nodeHealth.TakeDamage(_damage);
                    HandlePierce();
                }
            }
        }

        /// <summary>
        /// 관통 처리: 관통 횟수를 소비하고 남은 횟수가 없으면 Pool로 반환합니다.
        /// </summary>
        private void HandlePierce()
        {
            if (_currentPierceCount >= _pierceCount)
            {
                ReturnToPool();
            }
            else
            {
                _currentPierceCount++;
            }
        }

        /// <summary>
        /// 발사체를 비활성화하고 Pool로 반환합니다.
        /// </summary>
        private void ReturnToPool()
        {
            gameObject.SetActive(false);
            _onReturn?.Invoke(this);
        }
    }
}
