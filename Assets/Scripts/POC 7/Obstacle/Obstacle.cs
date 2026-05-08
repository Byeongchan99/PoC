using UnityEngine;

namespace POC7
{
    /// <summary>
    /// 장애물의 충돌 횟수 관리와 파괴 처리를 담당하는 컴포넌트.
    /// Collider2D, Rigidbody2D, SpriteRenderer는 프리팹에서 미리 설정해야 한다.
    ///
    /// [프리팹 설정 가이드]
    /// 1. Layer: "Obstacle" 레이어로 설정 (PathCalculator 레이캐스트 감지에 필수)
    /// 2. Rigidbody2D: Body Type = Kinematic
    /// 3. Collider2D: isTrigger = false 권장 (트리거 설정과 무관하게 레이캐스트가 항상 감지함)
    ///    - Circle  → CircleCollider2D
    ///    - Square  → BoxCollider2D
    ///    - Triangle → PolygonCollider2D (3점 직접 배치)
    /// 4. SpriteRenderer: 원하는 스프라이트와 크기를 직접 설정
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Obstacle : MonoBehaviour
    {
        /// <summary>
        /// 파괴되기까지 필요한 플레이어 충돌 횟수.
        /// -1로 설정하면 무적(파괴 불가능) 장애물이 된다.
        /// </summary>
        [SerializeField] private int _maxHits = 3;

        private int _remainingHits;
        private SpriteRenderer _spriteRenderer;

        /// <summary>_maxHits가 0 미만이면 무적 상태.</summary>
        public bool IsIndestructible => _maxHits < 0;

        /// <summary>
        /// Rigidbody2D를 Kinematic으로 설정하고 초기 색상을 적용한다.
        /// </summary>
        private void Awake()
        {
            var rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;

            _remainingHits = _maxHits;
            _spriteRenderer = GetComponent<SpriteRenderer>();

            UpdateColor();
        }

        /// <summary>
        /// 플레이어가 이 장애물에 충돌했을 때 PlayerController가 호출한다.
        /// 무적이면 무시하고, 남은 횟수를 차감하여 0 이하가 되면 오브젝트를 비활성화한다.
        /// </summary>
        public void RegisterHit()
        {
            if (IsIndestructible)
                return;

            _remainingHits--;
            UpdateColor();

            if (_remainingHits <= 0)
                gameObject.SetActive(false);
        }

        /// <summary>
        /// 남은 충돌 횟수 비율에 따라 SpriteRenderer 색상을 갱신한다.
        /// 무적이면 중간 회색, 파괴 가능이면 흰색(체력 최대) → 거의 검정(체력 최소)으로 표시한다.
        /// </summary>
        private void UpdateColor()
        {
            if (_spriteRenderer == null)
                return;

            if (IsIndestructible)
            {
                _spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f);
                return;
            }

            float ratio = _maxHits > 0 ? (float)_remainingHits / _maxHits : 0f;
            // 흰색(체력 최대) → 어두운 회색(체력 최소)으로 변화한다.
            _spriteRenderer.color = Color.Lerp(new Color(0.15f, 0.15f, 0.15f), Color.white, ratio);
        }
    }
}
