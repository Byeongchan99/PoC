using System;
using System.Collections.Generic;
using UnityEngine;

namespace POC6
{
    /// <summary>
    /// 플레이어가 획득한 카드 덱을 관리합니다.
    /// 웨이브 클리어 후 카드 선택 UI를 호출하고, 선택된 카드를 덱에 추가합니다.
    /// POC 기준: 카드 = 배치 가능한 노드 추가권. 손패/턴 시스템 없음.
    /// </summary>
    public class DeckManager : MonoBehaviour
    {
        [Header("시작 덱 설정")]
        [Tooltip("게임 시작 시 기본으로 제공되는 카드 목록")]
        [SerializeField] private List<CardData> _startingDeck = new();

        [Header("참조")]
        [SerializeField] private CardSelectionUI _cardSelectionUI;
        [SerializeField] private NodePlacer _nodePlacer;

        /// <summary>덱에 카드가 추가될 때 발행됩니다.</summary>
        public static event Action<CardData> OnCardAdded;

        /// <summary>덱이 변경될 때 발행됩니다. (추가/제거/초기화 모두 포함) DeckUI가 구독합니다.</summary>
        public static event Action OnDeckChanged;

        // 현재 덱 (획득한 카드 목록)
        private List<CardData> _deck = new();

        // 배치 중인 카드. 취소 시 덱으로 반환하기 위해 보관합니다.
        private CardData _pendingCard;

        /// <summary>현재 덱의 읽기 전용 카드 목록</summary>
        public IReadOnlyList<CardData> Deck => _deck;

        private void OnEnable()
        {
            NodePlacer.OnPlacementCancelled += ReturnPendingCard;
            NodePlacer.OnPlacementCompleted += ClearPendingCard;
        }

        private void OnDisable()
        {
            NodePlacer.OnPlacementCancelled -= ReturnPendingCard;
            NodePlacer.OnPlacementCompleted -= ClearPendingCard;
        }

        /// <summary>
        /// 덱을 시작 덱으로 초기화합니다.
        /// </summary>
        public void Initialize()
        {
            _deck = new List<CardData>(_startingDeck);
            _pendingCard = null;
            OnDeckChanged?.Invoke();
        }

        /// <summary>
        /// 카드를 덱에 추가합니다.
        /// </summary>
        public void AddCard(CardData card)
        {
            if (card == null) return;
            _deck.Add(card);
            OnCardAdded?.Invoke(card);
            OnDeckChanged?.Invoke();
        }

        /// <summary>
        /// 카드 선택 UI를 열어 플레이어에게 선택지를 제시합니다.
        /// 선택 완료 시 GameManager.OnCardSelected가 호출됩니다.
        /// </summary>
        public void ShowCardSelection(List<CardData> choices)
        {
            _cardSelectionUI?.Show(choices);
        }

        /// <summary>
        /// 덱에서 카드를 꺼내 NodePlacer로 배치 모드를 시작합니다.
        /// Build Phase에서 플레이어가 카드를 선택할 때 호출합니다.
        /// 배치가 취소되면 OnPlacementCancelled 이벤트로 카드가 자동 반환됩니다.
        /// </summary>
        public void UseCardForPlacement(CardData card)
        {
            if (!_deck.Contains(card)) return;
            if (card.NodeToGive == null) return;

            _pendingCard = card;
            _deck.Remove(card);
            OnDeckChanged?.Invoke();
            _nodePlacer.BeginPlacement(card.NodeToGive);
        }

        /// <summary>
        /// 배치가 취소됐을 때 NodePlacer.OnPlacementCancelled 이벤트에서 호출됩니다.
        /// 보관 중인 카드를 덱으로 돌려줍니다.
        /// </summary>
        private void ReturnPendingCard()
        {
            if (_pendingCard == null) return;

            _deck.Add(_pendingCard);
            _pendingCard = null;
            OnDeckChanged?.Invoke();
        }

        /// <summary>
        /// 배치가 성공적으로 완료됐을 때 NodePlacer.OnPlacementCompleted 이벤트에서 호출됩니다.
        /// 보관 중인 카드 참조를 정리합니다.
        /// </summary>
        private void ClearPendingCard()
        {
            _pendingCard = null;
        }

        /// <summary>
        /// 현재 덱의 카드 이름 목록을 반환합니다. 스냅샷 저장에 사용합니다.
        /// </summary>
        public List<string> GetCardNames()
        {
            var names = new List<string>();
            foreach (var card in _deck)
            {
                if (card != null)
                    names.Add(card.name);
            }
            return names;
        }

        /// <summary>
        /// 카드 이름 목록으로 덱을 복원합니다. 스냅샷 복원 시 사용합니다.
        /// </summary>
        public void RestoreFromSnapshot(List<string> cardNames)
        {
            _deck.Clear();
            _pendingCard = null;

            foreach (var name in cardNames)
            {
                CardData card = Resources.Load<CardData>($"POC6/Cards/{name}");
                if (card != null)
                    _deck.Add(card);
                else
                    Debug.LogWarning($"[DeckManager] CardData '{name}'을(를) Resources에서 찾을 수 없습니다.");
            }
        }
    }
}
