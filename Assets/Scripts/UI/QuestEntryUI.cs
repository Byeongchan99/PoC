using TMPro;
using UnityEngine;

/// <summary>
/// 퀘스트 목록 패널에 표시되는 개별 퀘스트 항목 UI.
/// 연결된 City가 제거되면 스스로 삭제됨.
/// </summary>
public class QuestEntryUI : MonoBehaviour
{
    [Header("UI 텍스트")]
    [SerializeField] private TextMeshProUGUI _headerText;
    [SerializeField] private TextMeshProUGUI _cargoProgressText;
    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private TextMeshProUGUI _statusText;

    private City _city;

    /// <summary>
    /// QuestListUI가 항목을 생성할 때 호출.
    /// 표시할 도시와 항목 번호를 설정.
    /// </summary>
    public void Initialize(City city, int questNumber)
    {
        _city = city;
        _headerText.text = $"퀘스트 {questNumber}";
    }

    private void Update()
    {
        // 도시 오브젝트가 씬에서 제거되면 이 항목도 함께 제거
        if (_city == null)
        {
            Destroy(gameObject);
            return;
        }

        RefreshUI();
    }

    /// <summary>
    /// 퀘스트 현황(납품량, 타이머, 상태)을 현재 데이터로 갱신.
    /// </summary>
    private void RefreshUI()
    {
        Quest quest = _city.Quest;

        _cargoProgressText.text = $"{quest.DeliveredCargo} / {quest.RequiredCargo}";
        _timerText.text = $"{quest.RemainingTime:F1}s";

        switch (quest.Status)
        {
            case QuestStatus.InProgress:
                _statusText.text = "진행 중";
                _statusText.color = Color.white;
                break;
            case QuestStatus.Success:
                _statusText.text = "성공!";
                _statusText.color = Color.green;
                break;
            case QuestStatus.Failed:
                _statusText.text = "실패";
                _statusText.color = Color.red;
                break;
        }
    }
}
