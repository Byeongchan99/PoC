using UnityEngine;
using UnityEngine.InputSystem;

public class SwordMouseTracking : MonoBehaviour
{
    [Tooltip("위치 추적 부드러움 (높을수록 빠르게 따라감)")]
    [Range(1f, 30f)]
    public float positionSmoothSpeed = 10f;

    [Tooltip("회전 부드러움 (높을수록 빠르게 회전함)")]
    [Range(1f, 30f)]
    public float rotationSmoothSpeed = 12f;

    [Tooltip("검 스프라이트 방향 보정 (위쪽=-90, 오른쪽=0)")]
    public float angleOffset = -90f;

    [Tooltip("스프라이트 중심에서 검 끝까지의 거리 (검 끝이 마우스를 따라감)")]
    public float tipOffset = 0.5f;

    private Camera _mainCamera;
    private float _currentAngle;
    private float _targetAngle;

    void Awake()
    {
        _mainCamera = Camera.main;
        _currentAngle = transform.eulerAngles.z;
        _targetAngle = _currentAngle;
    }

    void Update()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseScreen = new Vector3(mouseScreenPos.x, mouseScreenPos.y, Mathf.Abs(_mainCamera.transform.position.z));
        Vector2 mouseWorld = _mainCamera.ScreenToWorldPoint(mouseScreen);

        // 회전: 이동 방향을 향해 부드럽게 회전
        Vector2 moveDir = mouseWorld - (Vector2)transform.position;
        if (moveDir.sqrMagnitude > 0.001f)
            _targetAngle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;

        _currentAngle = Mathf.LerpAngle(_currentAngle, _targetAngle, rotationSmoothSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, 0f, _currentAngle + angleOffset);

        // 위치: 검 끝(tip)이 마우스에 오도록 중심을 뒤로 밀어서 이동
        Vector2 tipDir = transform.up;
        Vector2 targetPos = mouseWorld - tipDir * tipOffset;
        transform.position = Vector2.Lerp(transform.position, targetPos, positionSmoothSpeed * Time.deltaTime);
    }
}
