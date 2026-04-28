using UnityEngine;

namespace POC5.Data
{
    /// <summary>
    /// 자원 종류별 정적 메타데이터를 담는 ScriptableObject.
    /// 자원의 이름, 아이콘, 판매 가격 등 코드가 아닌 에셋에서 밸런싱할 수 있다.
    ///
    /// 에셋 생성: 프로젝트 창 우클릭 → Create → POC5 → Data → ResourceData
    /// </summary>
    [CreateAssetMenu(fileName = "ResourceData_New", menuName = "POC5/Data/ResourceData")]
    public class ResourceData : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("이 데이터가 나타내는 자원의 종류.")]
        [SerializeField] private ResourceType _resourceType;

        [Tooltip("UI에 표시할 자원 이름 (예: 물, 씨앗).")]
        [SerializeField] private string _displayName;

        [Tooltip("UI에 표시할 아이콘 스프라이트.")]
        [SerializeField] private Sprite _icon;

        [Header("판매 정보")]
        [Tooltip("시장에서 이 자원 1개를 팔면 얻는 돈의 양.")]
        [SerializeField] private int _sellPrice = 1;

        /// <summary>이 데이터가 나타내는 자원의 종류.</summary>
        public ResourceType ResourceType => _resourceType;

        /// <summary>UI에 표시할 자원 이름.</summary>
        public string DisplayName => _displayName;

        /// <summary>UI에 표시할 아이콘 스프라이트.</summary>
        public Sprite Icon => _icon;

        /// <summary>시장에서 이 자원 1개를 팔면 얻는 돈.</summary>
        public int SellPrice => _sellPrice;
    }
}
