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

        // 현재 덱 (획득한 카드 목록)
        private List<CardData> _deck = new();

        /// <summary>현재 덱의 읽기 전용 카드 목록</summary>
        public IReadOnlyList<CardData> Deck => _deck;

        /// <summary>
        /// 덱을 시작 덱으로 초기화합니다.
        /// </summary>
        public void Initialize()
        {
            _deck = new List<CardData>(_startingDeck);
        }

        /// <summary>
        /// 카드를 덱에 추가합니다.
        /// </summary>
        public void AddCard(CardData card)
        {
            if (card == null) return;
            _deck.Add(card);
            OnCardAdded?.Invoke(card);
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
        /// </summary>
        public void UseCardForPlacement(CardData card)
        {
            if (!_deck.Contains(card)) return;
            if (card.NodeToGive == null) return;

            _deck.Remove(card);
            _nodePlacer.BeginPlacement(card.NodeToGive);
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
