using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 기관차의 이동과 조향을 담당.
/// 항상 앞(transform.up)으로 전진하며, 마우스가 있는 방향으로 회전.
/// 지나온 경로를 TrainManager에 기록하여 기차 칸들이 따라올 수 있게 함.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class TrainLocomotive : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private TrainManager _trainManager;
    [SerializeField] private TrainStats _stats;

    [Header("충돌 무시 설정")]
    [Tooltip("앞에서 몇 번째 칸까지의 충돌을 무시할지.\n기관차와 인접한 칸은 항상 가깝기 때문에 오탐 방지 필요.")]
    [SerializeField] private int _collisionIgnoreCount = 1;

    [Header("현재 상태 (Inspector 확인용 - 읽기 전용)")]
    [SerializeField] private float _currentSpeed;
    [SerializeField] private float _currentAngle;

    private Rigidbody2D _rb;
    private Vector3 _previousPosition;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        // 물리 엔진 충돌 연산은 사용하지 않고 위치/회전만 직접 제어
        _rb.bodyType = RigidbodyType2D.Kinematic;

        _previousPosition = transform.position;
        _currentAngle = transform.eulerAngles.z;
    }

    private void FixedUpdate()
    {
        RotateTowardMouse();
        MoveForward();
        RecordCurrentPath();
    }

    /// <summary>
    /// 마우스 커서 방향을 향해 기관차를 회전시킴.
    /// 최대 회전 속도(_stats.RotationSpeed)를 넘지 않도록 제한.
    /// </summary>
    private void RotateTowardMouse()
    {
        // 스크린 좌표(픽셀)를 월드 좌표로 변환
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);

        Vector2 direction = (mouseWorldPos - transform.position).normalized;

        // Atan2: 두 점 사이의 방향을 각도(라디안)로 계산 후 도(degree)로 변환
        // -90을 빼는 이유: Atan2는 오른쪽(X축)을 0도로 계산하지만,
        // Unity의 2D 오브젝트는 위쪽(Y축)이 전방이므로 보정 필요
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

        // 현재 각도에서 목표 각도로 최대 회전 속도만큼만 이동
        _currentAngle = Mathf.MoveTowardsAngle(
            _currentAngle,
            targetAngle,
            _stats.RotationSpeed * Time.fixedDeltaTime
        );

        _rb.MoveRotation(_currentAngle);
    }

    /// <summary>
    /// 현재 바라보는 방향(transform.up)으로 기관차를 전진시킴.
    /// </summary>
    private void MoveForward()
    {
        Vector2 newPosition = _rb.position + (Vector2)transform.up * _currentSpeed * Time.fixedDeltaTime;
        _rb.MovePosition(newPosition);
    }

    /// <summary>
    /// 이번 프레임에 이동한 거리와 현재 위치/회전을 TrainManager에 기록.
    /// 기차 칸들이 이 기록을 참조하여 경로를 따라 이동.
    /// </summary>
    private void RecordCurrentPath()
    {
        float distanceMoved = Vector3.Distance(transform.position, _previousPosition);
        _trainManager.RecordPath(transform.position, transform.rotation, distanceMoved);
        _previousPosition = transform.position;
    }

    /// <summary>
    /// TrainManager가 스탯을 재계산한 후 새 속도를 적용할 때 호출.
    /// </summary>
    public void SetSpeed(float speed)
    {
        _currentSpeed = speed;
    }

    /// <summary>
    /// 기관차의 Collider2D가 기차 칸과 겹쳤을 때 호출됨(트리거 방식).
    /// 충돌한 칸부터 뒤의 칸을 모두 제거.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<TrainCar>(out var car)) return;

        // 너무 가까운 칸(앞 N개)은 구조상 항상 인접해 있으므로 무시
        if (car.CarIndex <= _collisionIgnoreCount) return;

        _trainManager.RemoveCarsFrom(car.CarIndex);
    }
}
