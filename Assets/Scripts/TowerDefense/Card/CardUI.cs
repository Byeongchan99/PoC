using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace POC4
{
    /// <summary>
    /// 손패의 카드 한 장을 표시하는 UI 컴포넌트.
    /// HandUI가 카드 수만큼 프리팹을 인스턴스화하고 Setup()으로 초기화한다.
    ///
    /// 프리팹 구성 예시:
    ///   CardUI (Button + CardUI 스크립트)
    ///   └─ Background (Image) ← _background 연결
    ///   └─ Label (TMP_Text)   ← _label 연결
    /// </summary>
    public class CardUI : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector 노출 필드
        // -------------------------------------------------------

        [Header("UI References")]
        [Tooltip("카드 정보를 표시할 TMP_Text")]
        [SerializeField] private TMP_Text _label;

        [Tooltip("카드 코스트를 표시할 TMP_Text")]
        [SerializeField] private TMP_Text _costText;

        [Tooltip("선택 상태에 따라 색이 바뀌는 배경 Image")]
        [SerializeField] private Image _background;

        [Header("Colors")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _pendingColor = new Color(1f, 1f, 0.4f, 1f);

        // -------------------------------------------------------
        // 내부 상태
        // -------------------------------------------------------

        private int _index;
        private System.Action<int> _onClicked;

        // -------------------------------------------------------
        // 초기화
        // -------------------------------------------------------

        /// <summary>
        /// HandUI가 인스턴스화 직후 호출한다.
        /// 카드 데이터, 손패 인덱스, 선택 상태, 클릭 콜백을 받아 표시를 설정한다.
        /// </summary>
        public void Setup(CardData card, int index, bool isPending, System.Action<int> onClicked)
        {
            _index = index;
            _onClicked = onClicked;

            if (_label != null)
                _label.text = BuildLabel(card);

            if (_costText != null)
                _costText.text = card.Cost.ToString();

            SetHighlight(isPending);

            Button btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnButtonClicked);
            }
        }

        /// <summary>
        /// 카드의 강조(선택) 상태를 갱신한다.
        /// HandUI에서 선택/취소 시 호출한다.
        /// </summary>
        public void SetHighlight(bool isPending)
        {
            if (_background != null)
                _background.color = isPending ? _pendingColor : _normalColor;
        }

        // -------------------------------------------------------
        // 내부 구현
        // -------------------------------------------------------

        private void OnButtonClicked()
        {
            _onClicked?.Invoke(_index);
        }

        /// <summary>
        /// 카드 종류와 효과에 따라 버튼에 표시할 텍스트를 생성한다.
        /// </summary>
        private string BuildLabel(CardData card)
        {
            if (card.Kind == CardData.CardKind.Wall && card.WallData != null)
            {
                string effect = card.WallData.EffectType == WallData.WallEffectType.None
                    ? "" : $"\n[{card.WallData.EffectType}]";
                return $"벽\n{card.WallData.Type}{effect}";
            }

            if (card.Kind == CardData.CardKind.Tower && card.TowerData != null)
            {
                string effect = card.TowerData.EffectType == TowerData.TowerEffectType.None
                    ? "" : $"\n[{card.TowerData.EffectType}]";
                return $"타워\n{card.TowerData.Type}{effect}";
            }

            return card.DisplayName;
        }
    }
}
