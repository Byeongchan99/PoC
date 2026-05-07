using UnityEngine;

namespace POC8
{
    /// <summary>
    /// 링의 회전을 담당하는 컴포넌트.
    /// A/D 키 입력을 받아 Z축 회전하며, 자식 오브젝트인 Player도 함께 회전한다.
    /// 플레이어 돌진 중에는 회전을 차단한다.
    /// </summary>
    public class RingController : MonoBehaviour
    {
        [SerializeField] private float _rotationSpeed = 90f;

        /// <summary>
        /// 매 프레임 A/D 키 입력을 감지하여 링을 회전한다.
        /// 플레이어가 Dashing 상태이면 입력을 무시한다.
        /// </summary>
        private void Update()
        {
            // 정적 프로퍼티로 확인하여 인스턴스 참조 없이 돌진 여부를 판단한다.
            if (PlayerController.IsPlayerDashing)
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

    }
}
