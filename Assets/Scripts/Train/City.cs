using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 맵에 배치되는 도시 오브젝트.
/// 퀘스트를 보유하며, 기관차가 도시 영역에 진입하면 화물을 자동으로 납품 처리함.
/// 퀘스트가 성공하면 기차 칸을 추가하고, 성공/실패 모두 일정 시간 후 스스로 제거됨.
/// </summary>
[RequireComponent(typeof(CircleCollider2D))]
public class City : MonoBehaviour
{
    [Header("도시 설정")]
    [Tooltip("퀘스트 종료 후 도시가 사라지기까지의 대기 시간 (초)")]
    [SerializeField] private float _destroyDelay = 3f;

    [Header("퀘스트 현황 (Inspector 확인용 - 읽기 전용)")]
    [SerializeField] private Quest _quest;

    private TrainManager _trainManager;

    // 도시가 제거될 때 CitySpawner에 알리기 위한 콜백
    private Action _onDestroyed;

    public Quest Quest => _quest;

    private void Awake()
    {
        GetComponent<CircleCollider2D>().isTrigger = true;
    }

    /// <summary>
    /// CitySpawner가 도시를 생성할 때 호출.
    /// 퀘스트 조건과 관리자 참조를 설정.
    /// </summary>
    public void Initialize(TrainManager trainManager, int requiredCargo, float timeLimit, Action onDestroyed)
    {
        _trainManager = trainManager;
        _quest = new Quest(requiredCargo, timeLimit);
        _onDestroyed = onDestroyed;
    }

    private void Update()
    {
        if (_quest == null || _quest.Status != QuestStatus.InProgress) return;

        _quest.UpdateTimer(Time.deltaTime);

        if (_quest.IsTimeUp())
            HandleQuestFailure();
    }

    /// <summary>
    /// 기관차가 도시 영역에 진입하면 화물 납품을 시도.
    /// 기관차가 아닌 오브젝트(기차 칸, 화물 등)는 무시.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_quest == null || _quest.Status != QuestStatus.InProgress) return;
        if (!other.TryGetComponent<TrainLocomotive>(out _)) return;

        TryDeliverCargo();
    }

    /// <summary>
    /// 현재 보유 화물에서 퀘스트 완료에 필요한 만큼만 납품.
    /// 납품 후 퀘스트가 완료되면 성공 처리.
    /// </summary>
    private void TryDeliverCargo()
    {
        int remaining = _quest.RequiredCargo - _quest.DeliveredCargo;

        // 납품 가능한 양: 보유 화물과 남은 요구량 중 작은 값
        int toDeliver = Mathf.Min(_trainManager.CurrentCargo, remaining);
        if (toDeliver <= 0) return;

        _trainManager.TryConsumeCargo(toDeliver);
        _quest.AddDelivery(toDeliver);

        if (_quest.Status == QuestStatus.Success)
            HandleQuestSuccess();
    }

    /// <summary>
    /// 퀘스트 성공 처리. 기차 칸을 추가하고 도시를 예약 제거.
    /// </summary>
    private void HandleQuestSuccess()
    {
        _trainManager.AddCar();
        StartCoroutine(DestroyAfterDelay());
    }

    /// <summary>
    /// 퀘스트 실패 처리. 별도 페널티 없이 도시를 예약 제거.
    /// </summary>
    private void HandleQuestFailure()
    {
        _quest.Fail();
        StartCoroutine(DestroyAfterDelay());
    }

    /// <summary>
    /// _destroyDelay 초 후 도시 오브젝트를 제거.
    /// 제거 전 CitySpawner에 콜백으로 알려 카운트를 갱신.
    /// </summary>
    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(_destroyDelay);
        _onDestroyed?.Invoke();
        Destroy(gameObject);
    }
}
