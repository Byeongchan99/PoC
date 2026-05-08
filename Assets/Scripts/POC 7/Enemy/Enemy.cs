using System;
using System.Collections;
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

        /// <summary>적이 초기화(스폰)될 때 발생하는 정적 이벤트. EnemyHealthHUD 등이 구독한다.</summary>
        public static event Action<Enemy> OnEnemySpawned;

        /// <summary>체력이 변경될 때마다 발생. 인자는 변경 후 체력. EnemyHealthHUD가 구독한다.</summary>
        public event Action<int> OnHealthChanged;

        [SerializeField] private int _maxHealth = 1;
        [SerializeField] private float _baseSize = 0.3f;

        /// <summary>
        /// 체력 1당 추가되는 시각적 크기. 기본값 기준 체력별 크기:
        /// 체력 2 → 0.5, 체력 4 → 0.7, 체력 8 → 1.1, 체력 16 → 1.9
        /// </summary>
        [SerializeField] private float _sizePerHealth = 0.1f;
        [SerializeField] private float _maxVisualSize = 2.0f;

        /// <summary>스폰 애니메이션(스케일 업 + 페이드 인) 재생 시간(초).</summary>
        [SerializeField] private float _spawnAnimDuration = 0.3f;

        private SpriteRenderer _spriteRenderer;
        private int _currentHealth;

        /// <summary>스폰 애니메이션이 재생 중이면 true. 피격 시 애니메이션을 즉시 종료하는 데 사용한다.</summary>
        private bool _isSpawning;

        /// <summary>현재 체력. 외부에서 읽기 전용.</summary>
        public int CurrentHealth => _currentHealth;

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

            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// Spawner가 적을 생성한 직후 호출한다. 체력을 설정하고 스폰 애니메이션을 시작한다.
        /// </summary>
        public void Initialize(int health)
        {
            _maxHealth = health;
            _currentHealth = health;

            // 애니메이션 시작 전 색상을 먼저 결정하되 알파를 0으로 설정한다.
            UpdateColor();
            SetAlpha(0f);
            transform.localScale = Vector3.zero;

            StartCoroutine(PlaySpawnAnimation());

            OnHealthChanged?.Invoke(_currentHealth);
            OnEnemySpawned?.Invoke(this);
        }

        /// <summary>
        /// 지정한 양만큼 체력을 감소시킨다. 체력이 0 이하가 되면 사망 처리한다.
        /// 스폰 애니메이션 중 피격 시 애니메이션을 즉시 종료하고 완전히 표시한다.
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (!IsAlive)
                return;

            if (_isSpawning)
                SnapSpawnAnimation();

            _currentHealth -= damage;
            UpdateVisualSize();
            UpdateColor();
            OnHealthChanged?.Invoke(Mathf.Max(0, _currentHealth));

            if (_currentHealth <= 0)
                Die();
        }

        /// <summary>
        /// 현재 체력에 비례하여 오브젝트 크기를 갱신한다. maxVisualSize를 초과하지 않도록 보정한다.
        /// 크기 공식: baseSize + currentHealth * sizePerHealth (상한: maxVisualSize)
        /// </summary>
        private void UpdateVisualSize()
        {
            float size = Mathf.Min(_baseSize + _currentHealth * _sizePerHealth, _maxVisualSize);
            transform.localScale = Vector3.one * size;
        }

        /// <summary>
        /// SpriteRenderer 색상의 알파값만 변경한다. 색상(HSV)은 유지된다.
        /// </summary>
        private void SetAlpha(float alpha)
        {
            if (_spriteRenderer == null)
                return;

            Color c = _spriteRenderer.color;
            c.a = alpha;
            _spriteRenderer.color = c;
        }

        /// <summary>
        /// 스폰 애니메이션을 재생한다.
        /// Smoothstep 보간을 사용해 시작과 끝에서 부드럽게 감속한다.
        /// </summary>
        private IEnumerator PlaySpawnAnimation()
        {
            _isSpawning = true;
            float targetSize = Mathf.Min(_baseSize + _currentHealth * _sizePerHealth, _maxVisualSize);
            float elapsed = 0f;

            while (elapsed < _spawnAnimDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _spawnAnimDuration);

                // Smoothstep: 시작과 끝에서 부드럽게 완급을 조절한다.
                float smooth = t * t * (3f - 2f * t);

                transform.localScale = Vector3.one * (targetSize * smooth);
                SetAlpha(smooth);

                yield return null;
            }

            transform.localScale = Vector3.one * targetSize;
            SetAlpha(1f);
            _isSpawning = false;
        }

        /// <summary>
        /// 스폰 애니메이션을 중단하고 최종 크기 및 불투명 상태로 즉시 전환한다.
        /// </summary>
        private void SnapSpawnAnimation()
        {
            StopAllCoroutines();
            _isSpawning = false;
            float targetSize = Mathf.Min(_baseSize + _currentHealth * _sizePerHealth, _maxVisualSize);
            transform.localScale = Vector3.one * targetSize;
            SetAlpha(1f);
        }

        /// <summary>
        /// 현재 체력의 2의 거듭제곱 지수(exponent)를 기반으로 색상을 결정한다.
        ///
        /// 황금비(0.618...)를 이용해 단계마다 색상환(Hue)을 순환시키면
        /// 인접한 단계끼리 색이 겹치지 않으면서 단계 수에 관계없이 다양한 색상이 나온다.
        /// 예: 지수 1→빨강(0.0), 2→하늘(0.618), 3→연두(0.236), 4→분홍(0.854), 5→청록(0.472) ...
        /// </summary>
        private void UpdateColor()
        {
            if (_spriteRenderer == null)
                return;

            int exponent = _currentHealth > 0 ? Mathf.FloorToInt(Mathf.Log(_currentHealth, 2)) : 0;

            // 황금비 켤레(golden ratio conjugate)를 곱할수록 색상환에서 균등하게 분산된 값을 얻는다.
            const float GoldenRatioConjugate = 0.6180339887f;
            float hue = (exponent * GoldenRatioConjugate) % 1f;

            _spriteRenderer.color = Color.HSVToRGB(hue, 0.85f, 0.95f);
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
