using System;
using UnityEngine;

namespace POC2
{

/// <summary>
/// 퀘스트의 현재 상태를 나타내는 열거형.
/// </summary>
public enum QuestStatus
{
    InProgress, // 진행 중
    Success,    // 성공
    Failed      // 실패 (시간 초과)
}

/// <summary>
/// 도시 퀘스트 데이터를 담는 클래스.
/// MonoBehaviour가 아닌 일반 C# 클래스로, City 컴포넌트가 보유함.
/// [Serializable]을 붙여 City의 Inspector에서 퀘스트 현황을 확인 가능.
/// </summary>
[Serializable]
public class Quest
{
    [Tooltip("퀘스트 완료에 필요한 총 화물 양")]
    [SerializeField] private int _requiredCargo;

    [Tooltip("주어진 제한 시간 (초)")]
    [SerializeField] private float _timeLimit;

    [Tooltip("남은 시간 (초)")]
    [SerializeField] private float _remainingTime;

    [Tooltip("현재까지 납품한 화물 양")]
    [SerializeField] private int _deliveredCargo;

    [Tooltip("퀘스트 현재 상태")]
    [SerializeField] private QuestStatus _status;

    public int RequiredCargo => _requiredCargo;
    public float RemainingTime => _remainingTime;
    public int DeliveredCargo => _deliveredCargo;
    public QuestStatus Status => _status;

    /// <summary>
    /// 퀘스트를 생성하고 초기 값을 설정.
    /// </summary>
    public Quest(int requiredCargo, float timeLimit)
    {
        _requiredCargo = requiredCargo;
        _timeLimit = timeLimit;
        _remainingTime = timeLimit;
        _status = QuestStatus.InProgress;
    }

    /// <summary>
    /// 매 프레임 타이머를 갱신. 진행 중인 퀘스트에만 적용.
    /// </summary>
    public void UpdateTimer(float deltaTime)
    {
        if (_status != QuestStatus.InProgress) return;

        _remainingTime = Mathf.Max(0f, _remainingTime - deltaTime);
    }

    /// <summary>
    /// 제한 시간이 만료되었는지 확인.
    /// </summary>
    public bool IsTimeUp() => _remainingTime <= 0f && _status == QuestStatus.InProgress;

    /// <summary>
    /// 화물을 납품하고 퀘스트 달성 여부를 갱신.
    /// 납품량이 요구량을 채우면 자동으로 Success 상태로 전환.
    /// </summary>
    public void AddDelivery(int amount)
    {
        if (_status != QuestStatus.InProgress) return;

        _deliveredCargo = Mathf.Min(_deliveredCargo + amount, _requiredCargo);

        if (_deliveredCargo >= _requiredCargo)
            _status = QuestStatus.Success;
    }

    /// <summary>
    /// 퀘스트를 실패 처리. 시간 초과 시 City에서 호출.
    /// </summary>
    public void Fail()
    {
        if (_status == QuestStatus.InProgress)
            _status = QuestStatus.Failed;
    }
}
}
