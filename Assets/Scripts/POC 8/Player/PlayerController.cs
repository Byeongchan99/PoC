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

        /// <summary>반사 횟수의 상한. UI 버튼으로 이 값을 초과할 수 없다.</summary>
        [SerializeField] private int _maxBounceCount = 10;

        private Rigidbody2D _rigidbody;
        private PlayerCombat _playerCombat;
        private PlayerState _currentState = PlayerState.Landed;

        /// <summary>현재 돌진 경로의 경유 지점 목록. StartDash 시 계산되며 순서대로 이동한다.</summary>
        private Vector2[] _waypoints;

        /// <summary>현재 이동 중인 경유 지점 인덱스.</summary>
        private int _currentWaypointIndex;

        /// <summary>현재 목표 경유 지점. UpdateDash에서 이 위치를 향해 이동한다.</summary>
        private Vector2 _dashTarget;

        /// <summary>
        /// 링 중심에서 플레이어 외벽까지의 거리. 씬에서 설정한 초기 localPosition 크기를 기준으로 삼는다.
        /// 플레이어 크기가 변해도 이 값(링 반경)은 유지되며, 부착 위치 계산에 사용한다.
        /// </summary>
        private float _ringRadius;

        /// <summary>현재 반사 횟수. UI 버튼으로 조절한다.</summary>
        private int _bounceCount;

        /// <summary>현재 돌진 중인지 외부에서 확인할 때 사용한다.</summary>
        public bool IsDashing => _currentState == PlayerState.Dashing;

        /// <summary>씬 내 플레이어가 돌진 중인지 나타내는 정적 프로퍼티.</summary>
        public static bool IsPlayerDashing { get; private set; }

        /// <summary>현재 반사 횟수. BounceCountUI가 읽는다.</summary>
        public int BounceCount => _bounceCount;

        /// <summary>
        /// Rigidbody2D를 Kinematic + Continuous 감지 모드로 초기화한다.
        /// Continuous 모드는 빠른 이동 시 적을 통과하는 터널링을 방지한다.
        /// </summary>
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _rigidbody.bodyType = RigidbodyType2D.Kinematic;

            // Discrete(기본값)는 각 물리 스텝의 끝 위치만 검사하여 빠른 오브젝트가 충돌체를 통과할 수 있다.
            // Continuous는 이동 경로 전체를 스윕 테스트하여 충돌 누락을 방지한다.
            _rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            _playerCombat = GetComponent<PlayerCombat>();

            // 링 중심(_ringTransform)에서 플레이어 초기 위치까지의 거리를 링 반경으로 저장한다.
            // PlayerController.Awake는 PlayerCombat.Awake보다 먼저 실행되므로
            // 이 시점의 scale은 씬 기본값(1.0)이다. 초기 반경(0.5)을 더해 실제 링 벽 반경을 구한다.
            Vector2 ringCenter = _ringTransform != null ? (Vector2)_ringTransform.position : Vector2.zero;
            float initialPlayerRadius = transform.localScale.x / 2f;
            _ringRadius = Vector2.Distance(transform.position, ringCenter) + initialPlayerRadius;
        }

        /// <summary>
        /// 모든 Awake가 완료된 후 초기 크기에 맞게 부착 위치를 조정한다.
        /// PlayerCombat.Awake가 초기 scale을 설정한 뒤 이 메서드가 실행되어야 정확하다.
        /// </summary>
        private void Start()
        {
            AdjustPositionToRingWall(transform.position);
        }

        /// <summary>
        /// 입력 처리는 Update에서, 이동은 FixedUpdate에서 실행한다.
        /// MovePosition은 FixedUpdate에서 호출해야 물리 엔진과 동기화되어 충돌 감지가 정확해진다.
        /// </summary>
        private void Update()
        {
            HandleInput();
        }

        /// <summary>
        /// MovePosition을 FixedUpdate에서 호출하여 물리 스텝과 이동을 동기화한다.
        /// </summary>
        private void FixedUpdate()
        {
            UpdateDash();
        }

        /// <summary>
        /// 반사 횟수를 1 증가시킨다. _maxBounceCount를 초과하지 않는다.
        /// </summary>
        public void IncreaseBounceCount()
        {
            _bounceCount = Mathf.Min(_bounceCount + 1, _maxBounceCount);
        }

        /// <summary>
        /// 반사 횟수를 1 감소시킨다. 0 미만으로 내려가지 않는다.
        /// </summary>
        public void DecreaseBounceCount()
        {
            _bounceCount = Mathf.Max(_bounceCount - 1, 0);
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
        /// 현재 위치에서 주어진 방향으로 출발하는 전체 경유 지점 목록을 계산한다.
        /// 반사 횟수(_bounceCount)만큼 반사를 추가하므로 총 (_bounceCount + 1)개의 지점이 생성된다.
        ///
        /// 각 교점에서 반사 방향은 Vector2.Reflect로 계산한다.
        ///   normal = (교점 - 링 중심).normalized   (링 내벽의 법선)
        ///   반사 방향 = Reflect(입사 방향, normal)
        /// </summary>
        private Vector2[] ComputeWaypoints(Vector2 startPos, Vector2 direction)
        {
            Vector2 ringCenter = _ringTransform != null ? (Vector2)_ringTransform.position : Vector2.zero;
            int totalPoints = _bounceCount + 1;
            Vector2[] waypoints = new Vector2[totalPoints];

            Vector2 pos = startPos;
            Vector2 dir = direction;

            for (int i = 0; i < totalPoints; i++)
            {
                float t = 2f * Vector2.Dot(ringCenter - pos, dir);

                // t가 너무 작으면 링 바깥 방향 클릭 등 비정상 상태. 남은 지점을 현재 위치로 채우고 종료한다.
                if (t < 0.1f)
                {
                    for (int j = i; j < totalPoints; j++)
                        waypoints[j] = pos;
                    break;
                }

                Vector2 nextPos = pos + t * dir;
                waypoints[i] = nextPos;

                if (i < totalPoints - 1)
                {
                    // 교점의 법선(링 중심 → 교점 방향)을 기준으로 방향을 반사한다.
                    Vector2 normal = (nextPos - ringCenter).normalized;
                    dir = Vector2.Reflect(dir, normal);
                    pos = nextPos;
                }
            }

            return waypoints;
        }

        /// <summary>
        /// 경유 지점을 계산하고 상태를 Dashing으로 전환한다.
        /// 각 구간마다 레이캐스트 공격 판정을 적용한 후 OnDashStarted 이벤트를 발생시킨다.
        /// </summary>
        private void StartDash(Vector2 firstTarget)
        {
            Vector2 startPos = transform.position;
            Vector2 direction = (firstTarget - startPos).normalized;

            _waypoints = ComputeWaypoints(startPos, direction);
            _currentWaypointIndex = 0;
            _dashTarget = _waypoints[0];

            _currentState = PlayerState.Dashing;
            IsPlayerDashing = true;

            if (_slashTrail != null && _trailEmitDuringDash)
                _slashTrail.emitting = true;

            // 모든 구간에 대해 레이캐스트 공격 판정을 적용한다.
            Vector2 segmentStart = startPos;
            foreach (Vector2 waypoint in _waypoints)
            {
                _playerCombat?.PerformDashAttack(segmentStart, waypoint);
                segmentStart = waypoint;
            }

            OnDashStarted?.Invoke();
        }

        /// <summary>
        /// Dashing 상태일 때 매 물리 스텝마다 현재 경유 지점을 향해 이동한다.
        /// 도달하면 다음 경유 지점으로 전환하고, 마지막 지점이면 Land()를 호출한다.
        /// </summary>
        private void UpdateDash()
        {
            if (_currentState != PlayerState.Dashing)
                return;

            Vector2 currentPos = _rigidbody.position;
            Vector2 newPos = Vector2.MoveTowards(currentPos, _dashTarget, _dashSpeed * Time.fixedDeltaTime);
            _rigidbody.MovePosition(newPos);

            if (Vector2.Distance(newPos, _dashTarget) >= 0.05f)
                return;

            _currentWaypointIndex++;

            if (_currentWaypointIndex >= _waypoints.Length)
                Land();
            else
                _dashTarget = _waypoints[_currentWaypointIndex];
        }

        /// <summary>
        /// 상태를 Landed로 전환하고 링 벽 부착 위치를 조정한 후 OnPlayerLanded 이벤트를 발생시킨다.
        /// </summary>
        private void Land()
        {
            _currentState = PlayerState.Landed;
            IsPlayerDashing = false;

            if (_slashTrail != null)
                _slashTrail.emitting = false;

            AdjustPositionToRingWall(_dashTarget);
            OnPlayerLanded?.Invoke();
        }

        /// <summary>
        /// 플레이어 외벽이 링 내벽에 정확히 닿도록 월드 좌표 위치를 조정한다.
        ///
        /// 목표 위치 = 링 중심 + 방향 * (링 반경 - 플레이어 반경)
        /// 플레이어 반경만큼 안쪽에 중심을 두므로 외벽이 링 내벽에 정확히 닿는다.
        /// </summary>
        private void AdjustPositionToRingWall(Vector2 landingWorldPos)
        {
            Vector2 ringCenter = _ringTransform != null ? (Vector2)_ringTransform.position : Vector2.zero;
            Vector2 dir = (landingWorldPos - ringCenter).normalized;
            if (dir.sqrMagnitude < 0.001f)
                return;

            float playerRadius = transform.localScale.x / 2f;
            Vector2 targetPos = ringCenter + dir * (_ringRadius - playerRadius);

            transform.position = targetPos;
            _rigidbody.position = targetPos;
        }
    }
}
