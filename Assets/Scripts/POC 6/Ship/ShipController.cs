using UnityEngine;
using UnityEngine.InputSystem;

namespace POC6
{
    /// <summary>
    /// 우주선의 조작을 담당합니다.
    /// - 마우스 위치를 향해 부드럽게 회전
    /// - 마우스 클릭 시 현재 바라보는 방향으로 전진 (Rigidbody2D 물리 기반)
    /// Build Phase에서는 조작이 비활성화됩니다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class ShipController : MonoBehaviour
    {
        [Header("테스트")]
        [Tooltip("체크하면 게임 시작 시 바로 조작 가능합니다. GameManager 없이 단독 테스트용.")]
        [SerializeField] private bool _enableOnStart = false;

        [Header("회전 설정")]
        [Tooltip("마우스를 향한 회전 속도 (도/초). 낮을수록 부드럽게 회전합니다.")]
        [Range(60f, 720f)]
        [SerializeField] private float _rotationSpeed = 180f;

        [Header("이동 설정")]
        [Tooltip("마우스 클릭 시 가해지는 추력 크기")]
        [Range(1f, 50f)]
        [SerializeField] private float _thrustForce = 15f;

        [Tooltip("최대 이동 속도 (유닛/초). 이 속도를 넘으면 추력이 억제됩니다.")]
        [Range(1f, 30f)]
        [SerializeField] private float _maxSpeed = 10f;

        [Tooltip("클릭을 떼고 나서 감속하는 선형 드래그 계수. 높을수록 빠르게 멈춥니다.")]
        [Range(0f, 5f)]
        [SerializeField] private float _linearDrag = 1.5f;

        // 조작 활성화 여부 (GameManager에서 Combat Phase일 때만 true)
        private bool _isControlEnabled = false;

        private Rigidbody2D _rigidbody;
        private Camera _mainCamera;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _mainCamera = Camera.main;

            _rigidbody.linearDamping = _linearDrag;
            // 2D 탑다운: 중력 필요 없음
            _rigidbody.gravityScale = 0f;
            // Z축 회전만 사용
            _rigidbody.constraints = RigidbodyConstraints2D.None;

            if (_enableOnStart)
                _isControlEnabled = true;
        }

        private void Update()
        {
            if (!_isControlEnabled) return;

            RotateTowardsMouse();
        }

        private void FixedUpdate()
        {
            if (!_isControlEnabled) return;

            // 마우스 좌클릭 유지 중에 계속 전진
            if (Mouse.current.leftButton.isPressed)
                ApplyThrust();
        }

        // ────────────────────────────────────────────────
        // 공개 API (GameManager에서 호출)
        // ────────────────────────────────────────────────

        /// <summary>
        /// 우주선 조작을 활성화합니다. Combat Phase 진입 시 GameManager에서 호출합니다.
        /// </summary>
        public void EnableControl()
        {
            _isControlEnabled = true;
        }

        /// <summary>
        /// 우주선 조작을 비활성화합니다. Build Phase 진입 시 GameManager에서 호출합니다.
        /// 관성은 그대로 유지되며 자연스럽게 감속합니다.
        /// </summary>
        public void DisableControl()
        {
            _isControlEnabled = false;
        }

        // ────────────────────────────────────────────────
        // 내부 로직
        // ────────────────────────────────────────────────

        /// <summary>
        /// 마우스 위치를 향해 우주선을 부드럽게 회전시킵니다.
        /// RotateTowards를 사용해서 최대 회전속도로 제한합니다.
        /// </summary>
        private void RotateTowardsMouse()
        {
            Vector3 mouseWorld = GetMouseWorldPosition();
            Vector2 direction = (mouseWorld - transform.position).normalized;

            // 방향 벡터를 각도로 변환 (2D에서 위쪽이 기본 방향)
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

            float currentAngle = transform.eulerAngles.z;
            float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, _rotationSpeed * Time.deltaTime);

            transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
        }

        /// <summary>
        /// 우주선이 현재 바라보는 방향(위쪽 벡터)으로 추력을 가합니다.
        /// 최대 속도를 초과하지 않도록 속도를 제한합니다.
        /// </summary>
        private void ApplyThrust()
        {
            // 현재 속도가 최대 속도 이하일 때만 추력 적용
            if (_rigidbody.linearVelocity.magnitude < _maxSpeed)
            {
                // transform.up은 우주선이 바라보는 방향 (회전 반영)
                _rigidbody.AddForce(transform.up * _thrustForce, ForceMode2D.Force);
            }
        }

        /// <summary>
        /// 현재 마우스 위치를 월드 좌표로 변환합니다.
        /// </summary>
        private Vector3 GetMouseWorldPosition()
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 pos = new Vector3(mouseScreenPos.x, mouseScreenPos.y, Mathf.Abs(_mainCamera.transform.position.z));
            return _mainCamera.ScreenToWorldPoint(pos);
        }
    }
}
