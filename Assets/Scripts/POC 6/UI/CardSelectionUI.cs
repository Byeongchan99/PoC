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
}
