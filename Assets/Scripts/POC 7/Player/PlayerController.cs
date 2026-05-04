using System;
using UnityEngine;

namespace POC7
{
    /// <summary>
    /// 플레이어의 상태(Landed/Dashing)와 돌진 이동을 담당하는 컴포넌트.
    /// 링의 자식 오브젝트로 배치하여 링 회전 시 함께 움직인다.
    /// </summary>
    // RingController보다 먼저 실행되도록 우선순위를 낮은 값으로 설정한다 (숫자가 낮을수록 먼저 실행)
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class PlayerController : MonoBehaviour
    {
        /// <summary>돌진이 시작될 때 발생. EnemySpawner가 구독한다.</summary>
        public static event Action OnDashStarted;

        /// <summary>착지가 완료될 때 발생. GameManager가 구독한다.</summary>
        public static event Action OnPlayerLanded;

        [SerializeField] private float _dashSpeed = 15f;

        /// <summary>링 오브젝트의 Transform. 원 중심 좌표(world space) 계산에 사용한다.</summary>
        [SerializeField] private Transform _ringTransform;

        /// <summary>
        /// 돌진 중 잔상을 표현하는 TrailRenderer. Player 오브젝트 또는 그 자식에 부착한다.
        ///
        /// [실무 권장]
        /// POC 단계에서는 TrailRenderer로 충분하다.
        /// 추후 Shader Graph 기반 잔상(Sprite 변형 + 알파 페이드) 또는
        /// VFX Graph의 GPU Particle로 업그레이드하면 더 풍부한 표현이 가능하다.
        /// </summary>
        [SerializeField] private TrailRenderer _slashTrail;

        /// <summary>false로 설정하면 항상 Trail을 emit한다. 디버그 용도.</summary>
        [SerializeField] private bool _trailEmitDuringDash = true;

        private Rigidbody2D _rigidbody;
        private PlayerState _currentState = PlayerState.Landed;
        private Vector2 _dashTarget;

        // StartDash와 UpdateDash가 같은 프레임에 실행되어 즉시 착지하는 것을 방지하는 플래그
        private bool _dashedThisFrame;

        /// <summary>현재 돌진 중인지 외부에서 확인할 때 사용한다.</summary>
        public bool IsDashing => _currentState == PlayerState.Dashing;

        /// <summary>
        /// 씬 내 플레이어가 돌진 중인지 나타내는 정적 프로퍼티.
        /// 인스턴스 참조 없이 어디서든 읽을 수 있다.
        /// </summary>
        public static bool IsPlayerDashing { get; private set; }

        /// <summary>
        /// Rigidbody2D를 Kinematic으로 설정하고 참조를 캐시한다.
        /// </summary>
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _rigidbody.bodyType = RigidbodyType2D.Kinematic;
        }

        private void Update()
        {
            _dashedThisFrame = false;
            HandleInput();

            // 이 프레임에 StartDash가 호출됐으면 UpdateDash는 다음 프레임부터 실행한다.
            // 같은 프레임에 StartDash → UpdateDash → Land가 연달아 호출되는 것을 방지한다.
            if (!_dashedThisFrame)
                UpdateDash();
        }

        /// <summary>
        /// Landed 상태에서만 마우스 클릭을 감지한다.
        /// 클릭 위치를 월드 좌표로 변환 후 돌진 목표 지점을 계산한다.
        /// </summary>
        private void HandleInput()
        {
            if (_currentState != PlayerState.Landed)
                return;

            if (!Input.GetMouseButtonDown(0))
                return;

            Vector3 screenPos = Input.mousePosition;
            screenPos.z = -Camera.main.transform.position.z;
            Vector2 clickWorldPos = Camera.main.ScreenToWorldPoint(screenPos);

            if (!TryCalculateDashTarget(clickWorldPos, out Vector2 target))
                return;

            StartDash(target);
        }

        /// <summary>
        /// 플레이어 위치(P)에서 클릭 방향(d)으로 나아갈 때 링 원의 반대편 교점을 계산한다.
        ///
        /// [수학 설명]
        /// P는 반경 r인 원 위의 점, C는 원 중심, d는 클릭 방향의 단위벡터.
        /// 직선의 방정식: Q = P + t * d
        /// 원의 방정식: |Q - C|^2 = r^2
        ///
        /// 대입 후 전개하면:
        ///   t^2 + 2t * dot(P - C, d) + (|P - C|^2 - r^2) = 0
        ///
        /// P가 원 위의 점이므로 |P - C| = r, 따라서 마지막 항은 0:
        ///   t * (t + 2 * dot(P - C, d)) = 0
        ///   t = 0  (출발점, 버림)
        ///   t = -2 * dot(P - C, d) = 2 * dot(C - P, d)  (반대편 교점)
        ///
        /// 주의: 이 단순화는 P가 정확히 원 위에 있을 때만 성립한다.
        /// </summary>
        /// <returns>유효한 목표 지점이 계산되면 true, 클릭 방향이 잘못됐으면 false</returns>
        private bool TryCalculateDashTarget(Vector2 clickWorldPos, out Vector2 target)
        {
            Vector2 playerPos = transform.position;
            Vector2 ringCenter = _ringTransform != null ? (Vector2)_ringTransform.position : Vector2.zero;

            Vector2 rawDir = clickWorldPos - playerPos;

            // 클릭 위치가 플레이어와 너무 가까우면 방향을 계산할 수 없다
            if (rawDir.sqrMagnitude < 0.0001f)
            {
                target = playerPos;
                return false;
            }

            Vector2 direction = rawDir.normalized;

            // 반대편 교점까지의 거리 t = 2 * dot(C - P, d)
            float t = 2f * Vector2.Dot(ringCenter - playerPos, direction);

            // t가 0 이하면 클릭 방향이 링 안쪽을 향하지 않으므로 돌진하지 않는다
            if (t < 0.1f)
            {
                target = playerPos;
                return false;
            }

            target = playerPos + t * direction;
            return true;
        }

        /// <summary>
        /// 돌진 목표 지점을 저장하고 상태를 Dashing으로 전환한다. OnDashStarted 이벤트를 발생시킨다.
        /// </summary>
        private void StartDash(Vector2 targetPos)
        {
            _dashTarget = targetPos;
            _currentState = PlayerState.Dashing;
            IsPlayerDashing = true;
            _dashedThisFrame = true;

            if (_slashTrail != null && _trailEmitDuringDash)
                _slashTrail.emitting = true;

            OnDashStarted?.Invoke();
        }

        /// <summary>
        /// Dashing 상태일 때 매 프레임 목표 지점을 향해 이동한다.
        /// 목표 지점에 충분히 가까워지면 Land()를 호출한다.
        /// </summary>
        private void UpdateDash()
        {
            if (_currentState != PlayerState.Dashing)
                return;

            // _rigidbody.position은 FixedUpdate 기준 값이므로 transform.position으로 읽는다
            Vector2 currentPos = transform.position;
            Vector2 newPos = Vector2.MoveTowards(currentPos, _dashTarget, _dashSpeed * Time.deltaTime);
            _rigidbody.MovePosition(newPos);

            // 목표 지점 도달 판정 (부동소수점 오차를 고려해 작은 임계값 사용)
            if (Vector2.Distance(newPos, _dashTarget) < 0.05f)
                Land();
        }

        /// <summary>
        /// 상태를 Landed로 전환하고 OnPlayerLanded 이벤트를 발생시킨다.
        /// 플레이어는 링의 자식이므로 위치를 그대로 유지하면 링 내벽에 부착된 상태가 된다.
        /// </summary>
        private void Land()
        {
            _currentState = PlayerState.Landed;
            IsPlayerDashing = false;

            if (_slashTrail != null)
                _slashTrail.emitting = false;

            OnPlayerLanded?.Invoke();
        }
    }
}
