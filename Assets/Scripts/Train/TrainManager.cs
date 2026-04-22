using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 기차 전체를 관리하는 중앙 컨트롤러.
/// 기관차가 지나온 경로를 기록하고, 기차 칸 추가/제거 및 화물 관리를 담당.
/// 씬에 하나만 존재하며, TrainLocomotive와 TrainCar가 이 컴포넌트를 참조함.
/// </summary>
public class TrainManager : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private TrainLocomotive _locomotive;
    [SerializeField] private GameObject _carPrefab;
    [SerializeField] private TrainStats _stats;

    [Header("현재 상태 (Inspector 확인용 - 읽기 전용)")]
    [SerializeField] private int _carCount;
    [SerializeField] private int _currentCargo;
    [SerializeField] private int _maxCargo;
    [SerializeField] private float _currentSpeed;

    private readonly List<TrainCar> _cars = new();

    // 기관차가 지나온 경로를 시간 순서대로 저장하는 목록
    // 각 항목은 위치, 회전, 누적 이동 거리를 포함
    private readonly List<PathPoint> _pathHistory = new();

    // 기관차가 게임 시작부터 이동한 총 거리
    private float _totalDistance;

    public int CurrentCargo => _currentCargo;
    public int MaxCargo => _maxCargo;
    public int CarCount => _carCount;

    private void Start()
    {
        RecalculateStats();
    }

    /// <summary>
    /// 기관차가 매 FixedUpdate마다 호출.
    /// 현재 위치와 회전을 경로 기록에 추가.
    /// </summary>
    public void RecordPath(Vector3 position, Quaternion rotation, float distanceMoved)
    {
        _totalDistance += distanceMoved;

        _pathHistory.Add(new PathPoint
        {
            Position = position,
            Rotation = rotation,
            Distance = _totalDistance
        });

        RemoveOldPathPoints();
    }

    /// <summary>
    /// 가장 뒤 칸보다 더 오래된 경로 포인트를 제거.
    /// 메모리를 지속적으로 낭비하지 않도록 필요한 구간만 유지.
    /// </summary>
    private void RemoveOldPathPoints()
    {
        // 마지막 칸 뒤로 여유분(2칸)을 두고 그 이전 데이터는 삭제
        float keepFromDistance = _totalDistance - (_carCount + 2) * _stats.CarSpacing;

        while (_pathHistory.Count > 1 && _pathHistory[0].Distance < keepFromDistance)
        {
            _pathHistory.RemoveAt(0);
        }
    }

    /// <summary>
    /// carIndex번 칸이 위치해야 할 PathPoint를 반환.
    /// 두 기록 포인트 사이를 보간하여 부드러운 이동을 제공.
    /// 히스토리 데이터가 부족하면 false를 반환.
    /// </summary>
    public bool TryGetPathPointForCar(int carIndex, out PathPoint result)
    {
        // 이 칸이 있어야 할 거리 = 현재 총 거리 - (칸 번호 * 칸 간격)
        float targetDistance = _totalDistance - carIndex * _stats.CarSpacing;

        // 히스토리를 최신 항목부터 역방향 탐색 (최근 데이터가 목록 뒤에 위치)
        for (int i = _pathHistory.Count - 1; i >= 0; i--)
        {
            if (_pathHistory[i].Distance > targetDistance) continue;

            // 다음 포인트가 있으면 두 점 사이를 보간하여 더 부드럽게 처리
            if (i + 1 < _pathHistory.Count)
            {
                PathPoint a = _pathHistory[i];
                PathPoint b = _pathHistory[i + 1];
                float span = b.Distance - a.Distance;
                float t = span > 0f ? (targetDistance - a.Distance) / span : 0f;

                result = new PathPoint
                {
                    Position = Vector3.Lerp(a.Position, b.Position, t),
                    Rotation = Quaternion.Slerp(a.Rotation, b.Rotation, t),
                    Distance = targetDistance
                };
            }
            else
            {
                result = _pathHistory[i];
            }

            return true;
        }

        result = default;
        return false;
    }

    /// <summary>
    /// 맨 뒤에 새 기차 칸을 추가.
    /// 퀘스트 성공 시 호출.
    /// </summary>
    public void AddCar()
    {
        GameObject carObj = Instantiate(_carPrefab);
        TrainCar car = carObj.GetComponent<TrainCar>();
        int newIndex = _cars.Count + 1;
        car.Initialize(this, newIndex);
        _cars.Add(car);
        _carCount = _cars.Count;
        RecalculateStats();
    }

    /// <summary>
    /// carIndex번 칸부터 그 뒤의 모든 칸을 제거.
    /// 기관차가 자신의 칸에 충돌했을 때 호출.
    /// </summary>
    public void RemoveCarsFrom(int carIndex)
    {
        // 뒤에서부터 제거해야 인덱스 오류 없이 안전하게 삭제 가능
        for (int i = _cars.Count - 1; i >= carIndex - 1; i--)
        {
            Destroy(_cars[i].gameObject);
            _cars.RemoveAt(i);
        }

        _carCount = _cars.Count;
        RecalculateStats();
    }

    /// <summary>
    /// 화물을 획득. 최대 화물 용량을 초과하지 않음.
    /// </summary>
    public void AddCargo(int amount)
    {
        _currentCargo = Mathf.Min(_currentCargo + amount, _maxCargo);
    }

    /// <summary>
    /// 화물을 소비. 보유 화물이 부족하면 false를 반환하고 소비하지 않음.
    /// </summary>
    public bool TryConsumeCargo(int amount)
    {
        if (_currentCargo < amount) return false;

        _currentCargo -= amount;
        return true;
    }

    /// <summary>
    /// 칸 수 변경 후 속도와 최대 화물 용량을 재계산하고 기관차에 적용.
    /// </summary>
    private void RecalculateStats()
    {
        _currentSpeed = _stats.CalculateSpeed(_carCount);
        _maxCargo = _stats.CalculateMaxCargo(_carCount);
        _locomotive.SetSpeed(_currentSpeed);
    }

    /// <summary>
    /// 기관차의 경로를 구성하는 한 지점.
    /// 위치, 회전, 출발점으로부터의 누적 이동 거리를 담음.
    /// </summary>
    public struct PathPoint
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float Distance;
    }
}
