using UnityEngine;
using UnityEngine.InputSystem;

namespace POC1
{
    public class SwordMouseTracking : MonoBehaviour
    {
        [Tooltip("검 스프라이트 방향 보정 (위쪽=-90, 오른쪽=0)")]
        public float angleOffset = -90f;

        [Tooltip("이 거리 이내에 들어오면 멈춤")]
        public float stopDistance = 0.1f;

        Camera _mainCamera;
        SwordStats _stats;
        float _currentAngle;

        void Awake()
        {
            _mainCamera = Camera.main;
            _stats = GetComponent<SwordStats>();
            _currentAngle = transform.eulerAngles.z;
        }

        void Update()
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Vector3 mouseScreen = new Vector3(mouseScreenPos.x, mouseScreenPos.y, Mathf.Abs(_mainCamera.transform.position.z));
            Vector2 mouseWorld = _mainCamera.ScreenToWorldPoint(mouseScreen);

            Vector2 toMouse = mouseWorld - (Vector2)transform.position;
            float dist = toMouse.magnitude;

            if (dist <= stopDistance)
                return;

            float targetAngle = Mathf.Atan2(toMouse.y, toMouse.x) * Mathf.Rad2Deg;
            _currentAngle = Mathf.LerpAngle(_currentAngle, targetAngle, _stats.rotationSpeed * Time.deltaTime);

            float speed = _stats.moveSpeed * Mathf.Clamp01(dist);
            Vector2 forward = new Vector2(Mathf.Cos(_currentAngle * Mathf.Deg2Rad), Mathf.Sin(_currentAngle * Mathf.Deg2Rad));
            transform.position += (Vector3)(forward * speed * Time.deltaTime);

            transform.rotation = Quaternion.Euler(0f, 0f, _currentAngle + angleOffset);
        }
    }
}
