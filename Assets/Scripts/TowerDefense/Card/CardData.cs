using UnityEngine;

namespace POC4
{
    /// <summary>
    /// 카드 한 장의 데이터를 담는 ScriptableObject.
    /// 벽 카드는 WallData를, 타워 카드는 TowerData를 참조한다.
    /// Create 메뉴: POC4 > Card Data
    /// </summary>
    [CreateAssetMenu(fileName = "CardData", menuName = "POC4/Card Data")]
    public class CardData : ScriptableObject
    {
        // -------------------------------------------------------
        // 카드 종류 열거형
        // -------------------------------------------------------

        public enum CardKind { Wall, Tower }

        // -------------------------------------------------------
        // Inspector 노출 필드
        // -------------------------------------------------------

        [Header("Card Info")]
        [Tooltip("벽 카드인지 타워 카드인지 지정한다.")]
        [SerializeField] private CardKind _kind = CardKind.Wall;

        [Tooltip("손패 UI에 표시할 이름. 비워두면 에셋 이름을 사용.")]
        [SerializeField] private string _displayName = "";

        [Header("Card Content")]
        [Tooltip("벽 카드일 때 참조하는 WallData. Kind = Wall 일 때만 사용.")]
        [SerializeField] private WallData _wallData;

        [Tooltip("타워 카드일 때 참조하는 TowerData. Kind = Tower 일 때만 사용.")]
        [SerializeField] private TowerData _towerData;

        // -------------------------------------------------------
        // 프로퍼티
        // -------------------------------------------------------

        public CardKind Kind => _kind;

        /// <summary>손패 UI에 표시할 카드 이름. 비어있으면 에셋 이름 사용.</summary>
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;

        public WallData WallData => _wallData;
        public TowerData TowerData => _towerData;

        // -------------------------------------------------------
        // 런타임 초기화 (카드 제작 시스템 전용)
        // -------------------------------------------------------

        /// <summary>
        /// 카드 제작 시스템이 ScriptableObject.CreateInstance 로 생성한 인스턴스에 WallData를 설정한다.
        /// 에디터에서 만든 에셋(CardData 파일)에는 절대 호출하지 말 것.
        /// </summary>
        public void Initialize(WallData wallData)
        {
            _kind = CardKind.Wall;
            _wallData = wallData;
            _towerData = null;
        }

        /// <summary>
        /// 카드 제작 시스템이 ScriptableObject.CreateInstance 로 생성한 인스턴스에 TowerData를 설정한다.
        /// 에디터에서 만든 에셋(CardData 파일)에는 절대 호출하지 말 것.
        /// </summary>
        public void Initialize(TowerData towerData)
        {
            _kind = CardKind.Tower;
            _towerData = towerData;
            _wallData = null;
        }

        // -------------------------------------------------------
        // 유효성
        // -------------------------------------------------------

        /// <summary>
        /// 카드 종류에 맞는 데이터가 연결되어 있는지 확인한다.
        /// </summary>
        public bool IsValid()
        {
            return _kind == CardKind.Wall ? _wallData != null : _towerData != null;
        }
    }
}
