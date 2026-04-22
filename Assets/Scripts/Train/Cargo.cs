using System;
using UnityEngine;

/// <summary>
/// 맵에 배치되는 화물 하나를 나타내는 컴포넌트.
/// 기관차와 충돌 시 TrainManager에 화물을 추가하고 스스로 제거됨.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class Cargo : MonoBehaviour
{
    [Header("화물 설정")]
    [Tooltip("이 화물을 수집했을 때 추가되는 화물 양")]
    [SerializeField] private int _amount = 10;

    private TrainManager _trainManager;

    // 화물이 수집되었을 때 CargoSpawner에 알리기 위한 콜백
    private Action _onCollected;

    public int Amount => _amount;

    /// <summary>
    /// CargoSpawner가 화물을 생성할 때 호출.
    /// 관리자 참조, 화물 수량, 수집 완료 콜백을 설정.
    /// </summary>
    public void Initialize(TrainManager trainManager, int amount, Action onCollected)
    {
        _trainManager = trainManager;
        _amount = amount;
        _onCollected = onCollected;
    }

    private void Awake()
    {
        // 기관차의 BoxCollider2D(Trigger)와 겹침 감지를 위해 반드시 Trigger로 설정
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    /// <summary>
    /// 기관차의 Collider가 이 화물에 진입했을 때 호출.
    /// 기관차가 아닌 다른 오브젝트(기차 칸 등)는 무시.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<TrainLocomotive>(out _)) return;

        _trainManager.AddCargo(_amount);
        _onCollected?.Invoke();
        Destroy(gameObject);
    }
}
