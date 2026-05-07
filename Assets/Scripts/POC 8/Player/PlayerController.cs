using System;
using System.Collections;
using UnityEngine;

namespace POC8
{
    /// <summary>
    /// 플레이어의 상태(Landed/Dashing)와 돌진 이동을 담당하는 컴포넌트.
    /// 링의 자식 오브젝트로 배치하여 링 회전 시 함께 움직인다.
    /// </summary>
    // RingController보다 먼저 실행되도록 우선순위를 낮은 값으로 설정한다 (숫자가 낮을수록 먼저 실행)
    [DefaultExecutionOrder(-10)]
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

        /// <summary>킬 리셋 발동 시 적용할 Time.timeScale 값.</summary>
        [SerializeField] private float _killResetTimeScale = 0.25f;

        /// <summary>킬 리셋 입력 대기 시간(실제 시간 기준, 초). 이 시간 안에 클릭하지 않으면 슬로우가 해제된다.</summary>
        [SerializeField] private float _killResetWindowDuration = 1.5f;

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

        /// <summary>킬 리셋 입력을 받을 수 있는 상태인지 나타낸다.</summary>
        private bool _killResetAvailable;

        /// <summary>실행 중인 킬 리셋 대기 코루틴. 새 리셋이 발동되기 전 중단하는 데 사용한다.</summary>
        private Coroutine _killResetCoroutine;

        /// <summary>
        /// 대시마다 증가하는 식별자. ApplyDamageDelayed 코루틴이 이 값을 검사해
        /// 새 대시가 시작된 경우 이전 경로의 데미지 예약을 무시한다.
        /// </summary>
        private int _dashId;

        /// <summary>현재 돌진 중인지 외부에서 확인할 때 사용한다.</summary>
        public bool IsDashing => _currentState == PlayerState.Dashing;

        /// <summary>씬 내 플레이어가 돌진 중인지 나타내는 정적 프로퍼티.</summary>
        public static bool IsPlayerDashing { get; private set; }

        /// <summary>현재 반사 횟수. BounceCountUI가 읽는다.</summary>
        public int BounceCount => _bounceCount;

        /// <summary>
        /// PlayerCombat 참조를 확보하고 링 반경을 초기화한다.
        /// PlayerController.Awake는 PlayerCombat.Awake보다 먼저 실행되므로
        /// 이 시점의 scale은 씬 기본값(1.0)이다. 초기 반경(0.5)을 더해 실제 링 벽 반경을 구한다.
        /// </summary>
        private void Awake()
        {
            _playerCombat = GetComponent<PlayerCombat>();

            Vector2 ringCenter = _ringTransform != null ? (Vector2)_ringTransform.position : Vector2.zero;
            float initialPlayerRadius = transform.localScale.x / 2f;
            _ringRadius = Vector2.Distance(transform.position, ringCenter) + initialPlayerRadius;
        }

        /// <summary>
        /// 킬 리셋 이벤트를 구독한다.
        /// </summary>
        private void OnEnable()
        {
            if (_playerCombat != null)
                _playerCombat.OnKillDuringDash += HandleKillDuringDash;
        }

        /// <summary>
        /// 킬 리셋 이벤트 구독을 해제하고 timeScale을 복구한다.
        /// </summary>
        private void OnDisable()
        {
            if (_playerCombat != null)
                _playerCombat.OnKillDuringDash -= HandleKillDuringDash;

            // 씬 전환 등으로 오브젝트가 비활성화될 때 timeScale이 느린 채로 남지 않도록 복구한다.
            if (_killResetAvailable)
                Time.timeScale = 1f;
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
        /// 입력 처리와 이동을 Update에서 실행한다.
        /// Time.deltaTime을 사용하므로 Time.timeScale 변화(슬로우)에 자동으로 동기화된다.
        /// </summary>
        private void Update()
        {
            HandleInput();
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
        /// Landed 상태이거나 킬 리셋 대기 중일 때 마우스 클릭을 감지한다.
        /// Input.GetMouseButtonDown은 Time.timeScale의 영향을 받지 않으므로 슬로우 중에도 정상 동작한다.
        /// </summary>
        private void HandleInput()
        {
            bool canInput = _currentState == PlayerState.Landed ||
                            (_currentState == PlayerState.Dashing && _killResetAvailable);

            if (!canInput)
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
        /// 플레이어 위치(P)에서 클릭 방향(d)으로 나아갈 때 링 내벽과의 교점을 계산한다.
        ///
        /// [수학 설명]
        /// Ray: Q = P + t * d  (d는 단위벡터)
        /// Circle: |Q - C|^2 = R^2  (R = 링 내벽 반경)
        ///
        /// 대입 후 전개 (u = P - C):
        ///   t^2 + 2*(u·d)*t + (|u|^2 - R^2) = 0
        ///   b = 2*(u·d),  c = |u|^2 - R^2
        ///   t = (-b + sqrt(b^2 - 4c)) / 2
        ///
        /// P가 링 내부에 있으면 c < 0이므로 판별식은 항상 양수(교점 2개).
        /// 양수 근을 선택하면 전방의 교점을 얻는다.
        /// P가 링 벽 위에 있으면 c = 0이 되어 t = -b로 단순화되며 기존 동작과 동일하다.
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
            float wallRadius = _ringRadius - transform.localScale.x / 2f;

            Vector2 u = playerPos - ringCenter;
            float b = 2f * Vector2.Dot(u, direction);
            float c = Vector2.Dot(u, u) - wallRadius * wallRadius;
            float discriminant = b * b - 4f * c;

            if (discriminant < 0f)
            {
                target = playerPos;
                return false;
            }

            float t = (-b + Mathf.Sqrt(discriminant)) / 2f;

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
        ///
        /// 교점 계산에 완전한 2차 방정식을 사용하므로 킬 리셋 직후처럼
        /// 출발 위치가 링 내부에 있는 경우에도 정확하게 동작한다.
        /// </summary>
        private Vector2[] ComputeWaypoints(Vector2 startPos, Vector2 direction)
        {
            Vector2 ringCenter = _ringTransform != null ? (Vector2)_ringTransform.position : Vector2.zero;
            float wallRadius = _ringRadius - transform.localScale.x / 2f;
            int totalPoints = _bounceCount + 1;
            Vector2[] waypoints = new Vector2[totalPoints];

            Vector2 pos = startPos;
            Vector2 dir = direction;

            for (int i = 0; i < totalPoints; i++)
            {
                // Ray-Circle 교점: t^2 + 2*(u·d)*t + (|u|^2 - R^2) = 0, u = pos - ringCenter
                Vector2 u = pos - ringCenter;
                float b = 2f * Vector2.Dot(u, dir);
                float c = Vector2.Dot(u, u) - wallRadius * wallRadius;
                float discriminant = b * b - 4f * c;
                float t = discriminant >= 0f ? (-b + Mathf.Sqrt(discriminant)) / 2f : 0f;

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
        /// _dashId를 증가시켜 이전 대시의 데미지 예약을 무효화하고,
        /// 각 구간을 CircleCastAll로 사전 계산하여 도달 시간에 맞춰 데미지를 예약한다.
        /// </summary>
        private void StartDash(Vector2 firstTarget)
        {
            if (_killResetAvailable)
                EndKillReset();

            _dashId++;
            int capturedDashId = _dashId;

            Vector2 startPos = transform.position;
            Vector2 direction = (firstTarget - startPos).normalized;

            _waypoints = ComputeWaypoints(startPos, direction);
            _currentWaypointIndex = 0;
            _dashTarget = _waypoints[0];

            _currentState = PlayerState.Dashing;
            IsPlayerDashing = true;

            if (_slashTrail != null && _trailEmitDuringDash)
                _slashTrail.emitting = true;

            // 각 구간의 히트를 사전 계산하고 도달 시간에 맞춰 데미지를 예약한다.
            Vector2 segmentStart = startPos;
            float timeOffset = 0f;
            foreach (Vector2 waypoint in _waypoints)
            {
                ScheduleSegmentDamage(segmentStart, waypoint, timeOffset, capturedDashId);
                timeOffset += Vector2.Distance(segmentStart, waypoint) / _dashSpeed;
                segmentStart = waypoint;
            }

            OnDashStarted?.Invoke();
        }

        /// <summary>
        /// 한 구간에 대해 CircleCastAll을 실행하고 각 히트에 대해 데미지 예약 코루틴을 시작한다.
        /// timeOffset은 이전 구간들을 통과하는 데 걸리는 누적 시간(게임 시간)이다.
        /// </summary>
        private void ScheduleSegmentDamage(Vector2 from, Vector2 to, float timeOffset, int dashId)
        {
            Vector2 direction = (to - from).normalized;
            float distance = Vector2.Distance(from, to);
            float radius = transform.localScale.x / 2f;

            RaycastHit2D[] hits = Physics2D.CircleCastAll(from, radius, direction, distance);
            foreach (RaycastHit2D hit in hits)
            {
                if (hit.collider.TryGetComponent(out IDamageable damageable))
                {
                    // 히트 지점까지의 도달 시간 = 이전 구간 누적 시간 + 이번 구간 내 거리 / 속도
                    float delay = timeOffset + hit.distance / _dashSpeed;
                    StartCoroutine(ApplyDamageDelayed(delay, damageable, dashId));
                }
            }
        }

        /// <summary>
        /// delay(게임 시간) 후 dashId가 현재와 일치하면 데미지를 적용한다.
        /// WaitForSeconds는 Time.timeScale을 따르므로 슬로우 중에는 대기 시간도 늘어나
        /// 플레이어의 실제 이동 속도와 자동으로 동기화된다.
        /// </summary>
        private IEnumerator ApplyDamageDelayed(float delay, IDamageable target, int dashId)
        {
            yield return new WaitForSeconds(delay);

            if (_dashId != dashId)
                yield break;

            _playerCombat.ApplyDamage(target);
        }

        /// <summary>
        /// Dashing 상태일 때 매 프레임마다 현재 경유 지점을 향해 이동한다.
        /// Time.deltaTime을 사용하므로 Time.timeScale 변화에 따라 이동 속도가 자동으로 조정된다.
        /// </summary>
        private void UpdateDash()
        {
            if (_currentState != PlayerState.Dashing)
                return;

            Vector2 currentPos = transform.position;
            Vector2 newPos = Vector2.MoveTowards(currentPos, _dashTarget, _dashSpeed * Time.deltaTime);
            transform.position = newPos;

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
            if (_killResetAvailable)
                EndKillReset();

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
        }

        /// <summary>
        /// 대시 중 킬이 발생했을 때 PlayerCombat으로부터 호출된다.
        /// 이미 킬 리셋 대기 중이면 타이머를 재시작하지 않는다(연속 킬 시 창이 리셋되지 않도록).
        /// </summary>
        private void HandleKillDuringDash()
        {
            if (_killResetAvailable)
                return;

            _killResetAvailable = true;
            _killResetCoroutine = StartCoroutine(KillResetWindow());
        }

        /// <summary>
        /// 킬 리셋 입력 대기 창을 실행한다.
        /// Time.timeScale을 낮춰 슬로우 효과를 적용하고, 대기 시간이 지나면 자동으로 해제한다.
        /// 타이머는 실제 시간(unscaledDeltaTime) 기준으로 동작한다.
        /// </summary>
        private IEnumerator KillResetWindow()
        {
            Time.timeScale = _killResetTimeScale;

            float elapsed = 0f;
            while (elapsed < _killResetWindowDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            EndKillReset();
        }

        /// <summary>
        /// 킬 리셋 상태를 정리하고 timeScale을 복구한다.
        /// 코루틴 만료, 클릭 입력, 착지 등 여러 경로에서 호출된다.
        /// </summary>
        private void EndKillReset()
        {
            if (_killResetCoroutine != null)
            {
                StopCoroutine(_killResetCoroutine);
                _killResetCoroutine = null;
            }

            _killResetAvailable = false;
            Time.timeScale = 1f;
        }
    }
}
