using UnityEngine;
using UnityEngine.InputSystem;

namespace POC3
{
    [DefaultExecutionOrder(-1)]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        // 링들을 자식으로 갖는 월드 컨테이너 (이 오브젝트를 회전시켜 플레이어 이동 효과 연출)
        [SerializeField] Transform worldContainer;

        float worldCurrentAngle;
        float worldTargetAngle;

        // 씬에 배치된 플레이어 위치 기준 고정 각도
        public float PlayerAngle { get; private set; }

        void Awake()
        {
            Instance = this;
            PlayerAngle = Mathf.Atan2(transform.position.y, transform.position.x) * Mathf.Rad2Deg;
        }

        void Update()
        {
            if (GameManager.Instance.CurrentState != GameManager.State.Playing) return;

            if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
                worldTargetAngle += 60f;
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
                worldTargetAngle -= 60f;

            worldCurrentAngle = Mathf.Lerp(worldCurrentAngle, worldTargetAngle, 12f * Time.deltaTime);
            worldContainer.rotation = Quaternion.Euler(0f, 0f, worldCurrentAngle);
        }
    }
}
