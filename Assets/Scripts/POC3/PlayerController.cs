using UnityEngine;
using UnityEngine.InputSystem;

namespace POC3
{
    [DefaultExecutionOrder(-1)]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [SerializeField] float orbitRadius = 3f;

        int targetSector = 4;
        float currentAngle = (4 + 0.5f) * 60f; // 270° = 화면 하단

        public float CurrentAngleDeg => currentAngle;

        void Awake() => Instance = this;

        void Update()
        {
            if (GameManager.Instance.CurrentState != GameManager.State.Playing) return;

            if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
                targetSector = (targetSector - 1 + 6) % 6;
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
                targetSector = (targetSector + 1) % 6;

            float targetAngle = (targetSector + 0.5f) * 60f;
            currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, 12f * Time.deltaTime);

            float rad = currentAngle * Mathf.Deg2Rad;
            transform.position = new Vector3(
                Mathf.Cos(rad) * orbitRadius,
                Mathf.Sin(rad) * orbitRadius,
                transform.position.z);

            // currentAngle + 90° → 로컬 +Y(팁)가 항상 중심을 향함
            transform.rotation = Quaternion.Euler(0f, 0f, currentAngle + 90f);
        }
    }
}
