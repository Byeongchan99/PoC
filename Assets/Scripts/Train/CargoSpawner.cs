using UnityEngine;

namespace POC2
{

/// <summary>
/// 일정 시간마다 맵에 화물을 랜덤 스폰하는 관리자.
/// 동시에 존재할 수 있는 최대 화물 수를 제한하여 화면이 너무 복잡해지지 않도록 함.
/// </summary>
public class CargoSpawner : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private TrainManager _trainManager;
    [SerializeField] private TrainLocomotive _locomotive;
    [SerializeField] private GameObject _cargoPrefab;

    [Header("스폰 설정")]
    [Tooltip("화물이 스폰되는 시간 간격 (초)")]
    [SerializeField] private float _spawnInterval = 3f;

    [Tooltip("맵에 동시에 존재할 수 있는 최대 화물 수")]
    [SerializeField] private int _maxCargoCount = 10;

    [Header("화물 수량 설정")]
    [Tooltip("화물 1개당 최소 화물 양")]
    [SerializeField] private int _minAmount = 5;

    [Tooltip("화물 1개당 최대 화물 양")]
    [SerializeField] private int _maxAmount = 30;

    [Header("스폰 위치 설정")]
    [Tooltip("카메라 화면 가장자리로부터의 여백 비율 (0~0.5). 클수록 중앙에만 스폰됨")]
    [SerializeField] private float _viewportMargin = 0.1f;

    [Tooltip("기관차로부터 이 거리 이내에는 스폰하지 않음 (기관차 바로 위에 스폰 방지)")]
    [SerializeField] private float _minDistanceFromLocomotive = 3f;

    [Header("현재 상태 (Inspector 확인용 - 읽기 전용)")]
    [SerializeField] private int _currentCargoCount;

    private float _spawnTimer;

    private void Update()
    {
        _spawnTimer += Time.deltaTime;

        if (_spawnTimer >= _spawnInterval)
        {
            _spawnTimer = 0f;
            TrySpawnCargo();
        }
    }

    /// <summary>
    /// 조건을 확인한 후 화물 스폰 시도.
    /// 최대 화물 수 초과 시 또는 적절한 위치를 찾지 못하면 스킵.
    /// </summary>
    private void TrySpawnCargo()
    {
        if (_currentCargoCount >= _maxCargoCount) return;

        if (!TryGetSpawnPosition(out Vector3 spawnPosition)) return;

        SpawnCargo(spawnPosition);
    }

    /// <summary>
    /// 기관차와 너무 가깝지 않은 랜덤 스폰 위치를 탐색.
    /// 최대 10번 시도 후 실패하면 false 반환.
    /// </summary>
    private bool TryGetSpawnPosition(out Vector3 result)
    {
        const int maxAttempts = 10;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 candidate = GetRandomViewportPosition();
            float distanceFromLocomotive = Vector2.Distance(candidate, _locomotive.transform.position);

            if (distanceFromLocomotive >= _minDistanceFromLocomotive)
            {
                result = candidate;
                return true;
            }
        }

        result = Vector3.zero;
        return false;
    }

    /// <summary>
    /// 카메라 뷰포트 안에서 여백을 고려한 랜덤 월드 좌표를 반환.
    /// </summary>
    private Vector3 GetRandomViewportPosition()
    {
        float x = Random.Range(_viewportMargin, 1f - _viewportMargin);
        float y = Random.Range(_viewportMargin, 1f - _viewportMargin);

        // 뷰포트 좌표(0~1)를 월드 좌표로 변환
        Vector3 worldPos = Camera.main.ViewportToWorldPoint(new Vector3(x, y, 0f));

        // 2D 게임이므로 z축은 0으로 고정
        worldPos.z = 0f;
        return worldPos;
    }

    /// <summary>
    /// 지정된 위치에 화물 오브젝트를 생성하고 초기화.
    /// </summary>
    private void SpawnCargo(Vector3 position)
    {
        GameObject cargoObj = Instantiate(_cargoPrefab, position, Quaternion.identity);
        Cargo cargo = cargoObj.GetComponent<Cargo>();

        int amount = Random.Range(_minAmount, _maxAmount + 1);

        // 화물이 수집될 때 현재 화물 수를 1 감소시키는 콜백 전달
        cargo.Initialize(_trainManager, amount, () => _currentCargoCount--);

        _currentCargoCount++;
    }
}
}
