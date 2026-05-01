using UnityEngine;

namespace POC6
{
    /// <summary>
    /// 카메라가 우주선을 부드럽게 추적합니다.
    /// Cinemachine 없이 직접 SmoothDamp로 구현했습니다.
    /// 탑다운 2D 뷰를 유지하며 Z 좌표는 변경하지 않습니다.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("추적 대상")]
        [Tooltip("추적할 우주선 Transform. 인스펙터에서 직접 연결하거나 GameManager가 주입합니다.")]
        [SerializeField] private Transform _target;

        [Header("추적 설정")]
        [Tooltip("카메라 이동 부드러움 정도 (초). 값이 클수록 천천히 따라갑니다.")]
        [Range(0.01f, 1f)]
        [SerializeField] private float _smoothTime = 0.15f;

        [Tooltip("카메라 Z 오프셋 (탑다운에서 카메라 높이). 음수 값이어야 합니다.")]
        [SerializeField] private float _zOffset = -10f;

        // SmoothDamp에 필요한 현재 속도 값 (내부 계산용)
        private Vector3 _velocity = Vector3.zero;

        private void LateUpdate()
        {
            if (_target == null) return;

            FollowTarget();
        }

        /// <summary>
        /// 추적 대상을 설정합니다. GameManager의 Init 단계에서 우주선 생성 후 호출합니다.
        /// </summary>
        public void SetTarget(Transform target)
        {
            _target = target;
        }

        /// <summary>
        /// SmoothDamp를 사용해 카메라가 대상을 부드럽게 추적합니다.
        /// </summary>
        private void FollowTarget()
        {
            Vector3 desiredPosition = new Vector3(
                _target.position.x,
                _target.position.y,
                _zOffset
            );

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref _velocity,
                _smoothTime
            );
        }
    }
}
