using UnityEngine;
using UnityEngine.InputSystem;

public class SwordMouseTracking : MonoBehaviour
{
    [Tooltip("검이 앞으로 나아가는 속도")]
    public float moveSpeed = 5f;

    [Tooltip("마우스 방향으로 회전하는 부드러움 (높을수록 빠르게 회전)")]
    [Range(1f, 30f)]
    public float rotationSmoothSpeed = 8f;

    [Tooltip("검 스프라이트 방향 보정 (위쪽=-90, 오른쪽=0)")]
    public float angleOffset = -90f;

    private Camera _mainCamera;
    private float _currentAngle;

    void Awake()
    {
        _mainCamera = Camera.main;
        _currentAngle = transform.eulerAngles.z;
    }

    void Update()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseScreen = new Vector3(mouseScreenPos.x, mouseScreenPos.y, Mathf.Abs(_mainCamera.transform.position.z));
        Vector2 mouseWorld = _mainCamera.ScreenToWorldPoint(mouseScreen);

        // 마우스 방향으로 목표 각도 계산
        Vector2 toMouse = mouseWorld - (Vector2)transform.position;
        if (toMouse.sqrMagnitude > 0.001f)
        {
            float targetAngle = Mathf.Atan2(toMouse.y, toMouse.x) * Mathf.Rad2Deg;
            _currentAngle = Mathf.LerpAngle(_currentAngle, targetAngle, rotationSmoothSpeed * Time.deltaTime);
        }

        // 마우스에 가까워질수록 속도를 줄여 도착 후 빙글빙글 도는 현상 방지
        float dist = toMouse.magnitude;
        float speed = moveSpeed * Mathf.Clamp01(dist);

        Vector2 forward = new Vector2(Mathf.Cos(_currentAngle * Mathf.Deg2Rad), Mathf.Sin(_currentAngle * Mathf.Deg2Rad));
        transform.position += (Vector3)(forward * speed * Time.deltaTime);

        transform.rotation = Quaternion.Euler(0f, 0f, _currentAngle + angleOffset);
    }
}
