using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 마우스 위치를 따라 부드럽게 움직이고 회전하는 검 컨트롤러.
/// 플레이어 주변을 공전하며 마우스 방향을 향해 회전합니다.
/// </summary>
public class SwordMouseTracking : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("검이 공전할 중심 오브젝트 (플레이어)")]
    public Transform pivot;

    [Header("공전 설정")]
    [Tooltip("피벗으로부터의 거리")]
    public float orbitRadius = 1.5f;

    [Header("부드러움 설정")]
    [Tooltip("위치 추적 부드러움 (높을수록 빠르게 따라감)")]
    [Range(1f, 30f)]
    public float positionSmoothSpeed = 10f;

    [Tooltip("회전 추적 부드러움 (높을수록 빠르게 회전함)")]
    [Range(1f, 30f)]
    public float rotationSmoothSpeed = 12f;

    [Header("검 오프셋")]
    [Tooltip("검의 기본 각도 오프셋 (스프라이트 방향 보정용)")]
    public float angleOffset = -90f;

    private Camera _mainCamera;
    private float _currentAngle;
    private float _targetAngle;

    void Awake()
    {
        _mainCamera = Camera.main;

        if (pivot == null)
            pivot = transform.parent;

        // 초기 각도를 현재 오브젝트 위치 기준으로 설정
        Vector2 initialDir = (Vector2)transform.position - (Vector2)(pivot != null ? pivot.position : Vector3.zero);
        _currentAngle = Mathf.Atan2(initialDir.y, initialDir.x) * Mathf.Rad2Deg;
        _targetAngle = _currentAngle;
    }

    void Update()
    {
        UpdateTargetAngle();
        SmoothMove();
    }

    void UpdateTargetAngle()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseScreen = new Vector3(mouseScreenPos.x, mouseScreenPos.y, Mathf.Abs(_mainCamera.transform.position.z));
        Vector2 mouseWorld = _mainCamera.ScreenToWorldPoint(mouseScreen);

        Vector2 pivotPos = pivot != null ? (Vector2)pivot.position : Vector2.zero;
        Vector2 direction = mouseWorld - pivotPos;

        if (direction.sqrMagnitude > 0.001f)
            _targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    void SmoothMove()
    {
        // 각도 보간 (360도 경계 처리 포함)
        _currentAngle = Mathf.LerpAngle(_currentAngle, _targetAngle, rotationSmoothSpeed * Time.deltaTime);

        float rad = _currentAngle * Mathf.Deg2Rad;
        Vector2 pivotPos = pivot != null ? (Vector2)pivot.position : Vector2.zero;

        // 목표 위치 계산
        Vector2 targetPosition = pivotPos + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * orbitRadius;

        // 위치 부드럽게 이동
        transform.position = Vector2.Lerp(transform.position, targetPosition, positionSmoothSpeed * Time.deltaTime);

        // 검이 마우스 방향을 향하도록 회전
        transform.rotation = Quaternion.Euler(0f, 0f, _currentAngle + angleOffset);
    }

    void OnDrawGizmosSelected()
    {
        if (pivot == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pivot.position, orbitRadius);
    }
}
