using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace POC6
{
    /// <summary>
    /// 웨이브 클리어 후 카드 3장을 표시하고 플레이어의 선택을 받습니다.
    /// 카드를 선택하면 GameManager.OnCardSelected를 호출합니다.
    /// </summary>
    public class CardSelectionUI : MonoBehaviour
    {
        [Header("UI 참조")]
        [Tooltip("카드 선택 패널 루트 오브젝트")]
        [SerializeField] private GameObject _panelRoot;

        [Tooltip("카드 버튼 프리팹. CardButtonEntry 컴포넌트가 있어야 합니다.")]
        [SerializeField] private CardButtonEntry _cardButtonPrefab;

        [Tooltip("카드 버튼들이 배치될 컨테이너 Transform")]
        [SerializeField] private Transform _cardContainer;

        // 현재 표시된 카드 버튼들
        private List<CardButtonEntry> _cardButtons = new();

        private void Awake()
        {
            _panelRoot.SetActive(false);
        }

        /// <summary>
        /// 카드 목록을 받아 선택 UI를 표시합니다. DeckManager에서 호출합니다.
        /// </summary>
        public void Show(List<CardData> choices)
        {
            // 기존 버튼 제거
            ClearCardButtons();

            foreach (var card in choices)
            {
                CardButtonEntry entry = Instantiate(_cardButtonPrefab, _cardContainer);
                entry.Setup(card, OnCardButtonClicked);
                _cardButtons.Add(entry);
            }

            _panelRoot.SetActive(true);
        }

        /// <summary>
        /// UI를 숨깁니다.
        /// </summary>
        public void Hide()
        {
            _panelRoot.SetActive(false);
            ClearCardButtons();
        }

        /// <summary>
        /// 카드 버튼 클릭 시 호출됩니다.
        /// </summary>
        private void OnCardButtonClicked(CardData selectedCard)
        {
            Hide();
            GameManager.Instance?.OnCardSelected(selectedCard);
        }

        private void ClearCardButtons()
        {
            foreach (var btn in _cardButtons)
            {
                if (btn != null)
                    Destroy(btn.gameObject);
            }
            _cardButtons.Clear();
        }
    }

    /// <summary>
    /// 카드 선택 UI에서 카드 하나를 표현하는 버튼 엔트리입니다.
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
