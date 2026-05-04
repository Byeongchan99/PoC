using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace POC6
{
    /// <summary>
    /// 카드 선택 UI에서 카드 하나를 표현하는 버튼 엔트리입니다.
    /// CardSelectionUI가 프리팹을 인스턴스화할 때 이 컴포넌트가 붙어 있어야 합니다.
    /// </summary>
    public class CardButtonEntry : MonoBehaviour
    {
        [SerializeField] private Image _artwork;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private Button _button;

        private CardData _card;
        private System.Action<CardData> _onClicked;

        private void Awake()
        {
            _button?.onClick.AddListener(HandleClick);
        }

        /// <summary>
        /// 카드 데이터를 바인딩하고 클릭 콜백을 설정합니다.
        /// </summary>
        public void Setup(CardData card, System.Action<CardData> onClicked)
        {
            _card = card;
            _onClicked = onClicked;

            if (_nameText != null) _nameText.text = card.CardName;
            if (_descriptionText != null) _descriptionText.text = card.Description;
            if (_artwork != null && card.CardArtwork != null) _artwork.sprite = card.CardArtwork;
        }

        private void HandleClick()
        {
            _onClicked?.Invoke(_card);
        }
    }
}
