using System.Collections.Generic;
using UnityEngine;

namespace POC6
{
    /// <summary>
    /// Build Phase에서 플레이어가 보유한 카드 목록을 표시합니다.
    /// 카드를 클릭하면 해당 노드의 배치 모드를 시작합니다.
    /// </summary>
    public class DeckUI : MonoBehaviour
    {
        [Header("UI 참조")]
        [Tooltip("덱 패널 루트 오브젝트. Build Phase에서만 표시됩니다.")]
        [SerializeField] private GameObject _panelRoot;

        [Tooltip("카드 버튼 프리팹. CardButtonEntry 컴포넌트가 있어야 합니다.")]
        [SerializeField] private CardButtonEntry _cardButtonPrefab;

        [Tooltip("카드 버튼들이 배치될 컨테이너 Transform")]
        [SerializeField] private Transform _cardContainer;

        [Header("참조")]
        [SerializeField] private DeckManager _deckManager;

        // 현재 표시된 카드 버튼 목록
        private List<CardButtonEntry> _cardButtons = new();

        private void Awake()
        {
            if (_panelRoot != null) _panelRoot.SetActive(false);
        }

        private void Start()
        {
            Debug.Log($"[DeckUI] Start - GameManager.Instance: {GameManager.Instance != null}, " +
                      $"State: {GameManager.Instance?.CurrentState}, " +
                      $"_panelRoot: {_panelRoot != null}, " +
                      $"_deckManager: {_deckManager != null}, " +
                      $"_cardButtonPrefab: {_cardButtonPrefab != null}");

            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.BuildPhase)
            {
                if (_panelRoot != null) _panelRoot.SetActive(true);
                RefreshDeck();
            }
        }

        private void OnEnable()
        {
            GameManager.OnGameStateChanged += HandleGameStateChanged;
            DeckManager.OnDeckChanged += RefreshDeck;
        }

        private void OnDisable()
        {
            GameManager.OnGameStateChanged -= HandleGameStateChanged;
            DeckManager.OnDeckChanged -= RefreshDeck;
        }

        /// <summary>
        /// 게임 상태가 바뀔 때 호출됩니다.
        /// Build Phase에서만 패널을 표시하고, 진입 시 덱 목록을 갱신합니다.
        /// </summary>
        private void HandleGameStateChanged(GameState state)
        {
            Debug.Log($"[DeckUI] HandleGameStateChanged: {state}, _panelRoot: {_panelRoot != null}");
            bool isBuild = state == GameState.BuildPhase;
            if (_panelRoot != null) _panelRoot.SetActive(isBuild);
            if (isBuild) RefreshDeck();
        }

        /// <summary>
        /// 현재 덱의 카드를 버튼으로 표시합니다.
        /// DeckManager.OnDeckChanged 이벤트 시 자동으로 호출됩니다.
        /// </summary>
        private void RefreshDeck()
        {
            Debug.Log($"[DeckUI] RefreshDeck - deckManager: {_deckManager != null}, " +
                      $"prefab: {_cardButtonPrefab != null}, " +
                      $"deckCount: {_deckManager?.Deck?.Count}");
            ClearButtons();

            if (_deckManager == null || _cardButtonPrefab == null) return;

            foreach (var card in _deckManager.Deck)
            {
                var entry = Instantiate(_cardButtonPrefab, _cardContainer);
                entry.Setup(card, OnCardClicked);
                _cardButtons.Add(entry);
            }
        }

        /// <summary>
        /// 카드 버튼 클릭 시 호출됩니다.
        /// DeckManager를 통해 해당 노드의 배치 모드를 시작합니다.
        /// 카드가 덱에서 제거되면 OnDeckChanged 이벤트로 목록이 자동 갱신됩니다.
        /// </summary>
        private void OnCardClicked(CardData card)
        {
            _deckManager.UseCardForPlacement(card);
        }

        private void ClearButtons()
        {
            foreach (var btn in _cardButtons)
                if (btn != null) Destroy(btn.gameObject);
            _cardButtons.Clear();
        }
    }
}
