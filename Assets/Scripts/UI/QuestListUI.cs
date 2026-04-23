using UnityEngine;

namespace POC2;

/// <summary>
/// 화면 우측에 고정되는 퀘스트 목록 패널.
/// CitySpawner가 도시를 생성할 때 AddEntry를 호출하여 항목을 추가.
/// 각 항목(QuestEntryUI)은 연결된 도시가 제거될 때 스스로 삭제됨.
/// </summary>
public class QuestListUI : MonoBehaviour
{
    [Header("참조")]
    [Tooltip("QuestEntryUI 컴포넌트를 가진 Prefab")]
    [SerializeField] private GameObject _entryPrefab;

    [Tooltip("항목들이 배치될 부모 오브젝트. Vertical Layout Group 컴포넌트 권장")]
    [SerializeField] private Transform _entryContainer;

    // 퀘스트 번호를 매기기 위한 증가 카운터
    private int _questCounter;

    /// <summary>
    /// 새 도시가 생성될 때 CitySpawner에서 호출.
    /// 해당 도시의 퀘스트 항목을 목록에 추가.
    /// </summary>
    public void AddEntry(City city)
    {
        _questCounter++;

        GameObject entryObj = Instantiate(_entryPrefab, _entryContainer);
        QuestEntryUI entry = entryObj.GetComponent<QuestEntryUI>();
        entry.Initialize(city, _questCounter);
    }
}
