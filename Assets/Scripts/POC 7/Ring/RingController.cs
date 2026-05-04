using UnityEngine;

namespace POC7
{
    /// <summary>
    /// 링의 회전을 담당하는 컴포넌트.
    /// A/D 키 입력을 받아 Z축 회전하며, 자식 오브젝트인 Player도 함께 회전한다.
    /// PlayerController의 이벤트를 구독하여 돌진 중에는 회전을 자동으로 비활성화한다.
    /// </summary>
    public class RingController : MonoBehaviour
    {
        [SerializeField] private float _rotationSpeed = 90f;

        /// <summary>
        /// 회전 가능 여부. Player가 Dashing 상태일 때 false로 설정된다.
        /// </summary>
        [SerializeField] private bool _canRotate = true;

        /// <summary>
        /// 오브젝트 활성화 시 PlayerController 이벤트를 구독한다.
        /// </summary>
        private void OnEnable()
        {
            PlayerController.OnDashStarted += OnDashStarted;
            PlayerController.OnPlayerLanded += OnPlayerLanded;
        }

        /// <summary>
        /// 오브젝트 비활성화 시 이벤트 구독을 해제한다.
        /// </summary>
        private void OnDisable()
        {
            PlayerController.OnDashStarted -= OnDashStarted;
            PlayerController.OnPlayerLanded -= OnPlayerLanded;
        }

        /// <summary>
        /// 매 프레임 A/D 키 입력을 감지하여 링을 회전한다.
        /// canRotate가 false이면 입력을 무시한다.
        /// </summary>
        private void Update()
        {
            if (!_canRotate)
                return;

            float input = 0f;

            if (Input.GetKey(KeyCode.A))
                input = 1f;   // A: 반시계 방향 (양의 Z 회전)
            else if (Input.GetKey(KeyCode.D))
                input = -1f;  // D: 시계 방향 (음의 Z 회전)

            if (input == 0f)
                return;

            transform.Rotate(0f, 0f, input * _rotationSpeed * Time.deltaTime);
        }

        /// <summary>
        /// 외부에서 링 회전 가능 여부를 설정한다.
        /// Player가 Dashing 상태에 진입/종료할 때 호출한다.
        /// </summary>
        public void SetRotationEnabled(bool enabled)
        {
            _canRotate = enabled;
        }

        /// <summary>
        /// 플레이어 돌진 시작 이벤트 수신. 링 회전을 비활성화한다.
        /// </summary>
        private void OnDashStarted()
        {
            SetRotationEnabled(false);
        }

        /// <summary>
        /// 플레이어 착지 이벤트 수신. 링 회전을 다시 활성화한다.
        /// </summary>
        private void OnPlayerLanded()
        {
            SetRotationEnabled(true);
        }
    }
}
