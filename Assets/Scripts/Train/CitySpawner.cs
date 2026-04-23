using UnityEngine;

namespace POC2;

/// <summary>
/// 일정 시간마다 맵에 도시를 랜덤 스폰하는 관리자.
/// 동시에 존재할 수 있는 최대 도시 수를 제한하여 맵이 너무 복잡해지지 않도록 함.
/// </summary>
public class CitySpawner : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private TrainManager _trainManager;
    [SerializeField] private TrainLocomotive _locomotive;
    [SerializeField] private GameObject _cityPrefab;
    [SerializeField] private QuestListUI _questListUI;

    [Header("스폰 설정")]
    [Tooltip("도시가 스폰되는 시간 간격 (초)")]
    [SerializeField] private float _spawnInterval = 15f;

    [Tooltip("맵에 동시에 존재할 수 있는 최대 도시 수")]
    [SerializeField] private int _maxCityCount = 3;

    [Header("퀘스트 조건 설정")]
    [Tooltip("퀘스트 최소 요구 화물 양")]
    [SerializeField] private int _minRequiredCargo = 50;

    [Tooltip("퀘스트 최대 요구 화물 양")]
    [SerializeField] private int _maxRequiredCargo = 200;

    [Tooltip("퀘스트 최소 제한 시간 (초)")]
    [SerializeField] private float _minTimeLimit = 30f;

    [Tooltip("퀘스트 최대 제한 시간 (초)")]
    [SerializeField] private float _maxTimeLimit = 60f;

    [Header("스폰 위치 설정")]
    [Tooltip("카메라 화면 가장자리로부터의 여백 비율 (0~0.5)")]
    [SerializeField] private float _viewportMargin = 0.1f;

    [Tooltip("기관차로부터 이 거리 이내에는 스폰하지 않음")]
    [SerializeField] private float _minDistanceFromLocomotive = 5f;

    [Header("현재 상태 (Inspector 확인용 - 읽기 전용)")]
    [SerializeField] private int _currentCityCount;

    private float _spawnTimer;

    private void Update()
    {
        _spawnTimer += Time.deltaTime;

        if (_spawnTimer >= _spawnInterval)
        {
            _spawnTimer = 0f;
            TrySpawnCity();
        }
    }

    /// <summary>
    /// 조건을 확인한 후 도시 스폰을 시도.
    /// 최대 도시 수 초과 시 또는 적절한 위치를 찾지 못하면 스킵.
    /// </summary>
    private void TrySpawnCity()
    {
        if (_currentCityCount >= _maxCityCount) return;
        if (!TryGetSpawnPosition(out Vector3 spawnPosition)) return;

        SpawnCity(spawnPosition);
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

            if (Vector2.Distance(candidate, _locomotive.transform.position) >= _minDistanceFromLocomotive)
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

        Vector3 worldPos = Camera.main.ViewportToWorldPoint(new Vector3(x, y, 0f));
        worldPos.z = 0f;
        return worldPos;
    }

    /// <summary>
    /// 지정된 위치에 도시를 생성하고 랜덤 퀘스트 조건으로 초기화.
    /// </summary>
    private void SpawnCity(Vector3 position)
    {
        GameObject cityObj = Instantiate(_cityPrefab, position, Quaternion.identity);
        City city = cityObj.GetComponent<City>();

        int requiredCargo = Random.Range(_minRequiredCargo, _maxRequiredCargo + 1);
        float timeLimit = Random.Range(_minTimeLimit, _maxTimeLimit);

        // 도시가 제거될 때 현재 도시 수를 1 감소시키는 콜백 전달
        city.Initialize(_trainManager, requiredCargo, timeLimit, () => _currentCityCount--);
        _questListUI.AddEntry(city);

        _currentCityCount++;
    }
}
