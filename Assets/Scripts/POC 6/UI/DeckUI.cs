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
            // 게임이 이미 BuildPhase인 상태로 시작하면 Update 첫 프레임 전에 즉시 표시
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.BuildPhase)
            {
                if (_panelRoot != null) _panelRoot.SetActive(true);
                RefreshDeck();
            }
        }

        private void OnEnable()
        {
            DeckManager.OnDeckChanged += RefreshDeck;
            GameManager.OnGameStateChanged += HandleGameStateChanged;

            // 부모 오브젝트가 비활성화됐다가 다시 활성화된 경우 즉시 상태 체크
            // (카드 선택 중 이벤트를 놓친 경우를 보완)
            bool isBuildPhase = GameManager.Instance != null &&
                                GameManager.Instance.CurrentState == GameState.BuildPhase;
            Debug.Log($"[DeckUI] OnEnable - isBuildPhase:{isBuildPhase}");
            if (isBuildPhase)
            {
                if (_panelRoot != null) _panelRoot.SetActive(true);
                RefreshDeck();
            }
        }

        private void OnDisable()
        {
            DeckManager.OnDeckChanged -= RefreshDeck;
            GameManager.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void Update()
        {
            bool shouldBeVisible = GameManager.Instance != null &&
                                   GameManager.Instance.CurrentState == GameState.BuildPhase;

            if (_panelRoot == null) return;

            if (_panelRoot.activeSelf != shouldBeVisible)
            {
                _panelRoot.SetActive(shouldBeVisible);
                if (shouldBeVisible) RefreshDeck();
            }
        }

        /// <summary>
        /// BuildPhase 진입 시 카드 목록을 명시적으로 갱신합니다.
        /// OnDeckChanged로 갱신이 지연된 경우를 보완합니다.
        /// </summary>
        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.BuildPhase) RefreshDeck();
        }

        /// <summary>
        /// 현재 덱의 카드를 버튼으로 표시합니다.
        /// DeckManager.OnDeckChanged 이벤트 시 자동으로 호출됩니다.
        /// </summary>
        private void RefreshDeck()
        {
            Debug.Log($"[DeckUI] RefreshDeck - deckManager:{_deckManager != null}, prefab:{_cardButtonPrefab != null}, deckCount:{_deckManager?.Deck?.Count}");
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
