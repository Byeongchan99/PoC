using UnityEngine;

/// <summary>
/// 개별 기차 칸.
/// TrainManager의 경로 히스토리에서 자신의 인덱스에 해당하는 지점을 읽어 이동.
/// 기관차와의 충돌 감지를 위해 Collider2D가 반드시 필요.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class TrainCar : MonoBehaviour
{
    [Header("현재 상태 (Inspector 확인용 - 읽기 전용)")]
    [Tooltip("1번이 기관차 바로 뒤 칸, 번호가 클수록 뒤에 위치")]
    [SerializeField] private int _carIndex;

    private TrainManager _trainManager;
    private Rigidbody2D _rb;

    public int CarIndex => _carIndex;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        // 물리 충돌 연산 없이 위치만 직접 제어
        // Kinematic이어야 기관차의 OnTriggerEnter2D가 정상 동작함
        _rb.bodyType = RigidbodyType2D.Kinematic;
    }

    /// <summary>
    /// TrainManager가 새 칸을 생성할 때 호출.
    /// 자신이 몇 번째 칸인지와 상위 관리자 참조를 설정.
    /// </summary>
    public void Initialize(TrainManager manager, int index)
    {
        _trainManager = manager;
        _carIndex = index;
    }

    private void FixedUpdate()
    {
        FollowPath();
    }

    /// <summary>
    /// TrainManager에서 자신의 인덱스에 해당하는 경로 지점을 받아 위치와 회전을 업데이트.
    /// 경로 데이터가 아직 충분히 쌓이지 않았으면 이동하지 않음.
    /// </summary>
    private void FollowPath()
    {
        if (!_trainManager.TryGetPathPointForCar(_carIndex, out var point)) return;

        _rb.MovePosition(point.Position);

        // Quaternion에서 2D 회전값(Z축 각도)만 추출하여 적용
        _rb.MoveRotation(point.Rotation.eulerAngles.z);
    }
}
