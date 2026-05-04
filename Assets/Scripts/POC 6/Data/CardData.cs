using UnityEngine;

namespace POC6
{
    /// <summary>
    /// 카드 한 장의 데이터를 담는 ScriptableObject입니다.
    /// 웨이브 클리어 후 카드 선택 화면에서 제시됩니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Card_New", menuName = "POC6/Data/CardData")]
    public class CardData : ScriptableObject
    {
        [Header("카드 정보")]
        [Tooltip("카드 이름 (예: 레이저 포탑 추가)")]
        [SerializeField] private string _cardName;

        [Tooltip("이 카드를 선택하면 우주선에 배치할 수 있게 되는 노드")]
        [SerializeField] private NodeData _nodeToGive;

        [Tooltip("카드 선택 UI에 표시되는 일러스트 스프라이트")]
        [SerializeField] private Sprite _cardArtwork;

        [Tooltip("카드 효과에 대한 설명 텍스트")]
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        // 읽기 전용 프로퍼티들
        public string CardName => _cardName;
        public NodeData NodeToGive => _nodeToGive;
        public Sprite CardArtwork => _cardArtwork;
        public string Description => _description;
    }
}
