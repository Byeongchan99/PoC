using System.Collections.Generic;
using UnityEngine;

namespace POC4
{
    /// <summary>
    /// 플레이어의 손패(보유 카드 목록)를 관리하는 클래스.
    /// 게임 시작 시 초기 손패를 지급하고, 카드 추가 및 제거 기능을 제공한다.
    /// POC에서 손패 최대 크기는 무제한.
    /// </summary>
    public class Hand : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector 노출 필드
        // -------------------------------------------------------

        [Header("Initial Hand Settings (게임 시작 시 자동 지급)")]
        [Tooltip("효과 없는 기본 벽 카드 데이터")]
        [SerializeField] private CardData _defaultWallCard;

        [Tooltip("효과 없는 기본 타워 카드 데이터")]
        [SerializeField] private CardData _defaultTowerCard;

        [Tooltip("시작 시 지급할 기본 벽 카드 수")]
        [SerializeField] private int _initialWallCardCount = 3;

        [Tooltip("시작 시 지급할 기본 타워 카드 수")]
        [SerializeField] private int _initialTowerCardCount = 3;

        [Header("Additional Starting Cards (선택, 위 기본 카드 외 추가 지급)")]
        [SerializeField] private List<CardData> _additionalStartingCards = new List<CardData>();

        // -------------------------------------------------------
        // 내부 상태
        // -------------------------------------------------------

        private readonly List<CardData> _cards = new List<CardData>();

        // -------------------------------------------------------
        // 프로퍼티
        // -------------------------------------------------------

        /// <summary>현재 손패에 있는 카드 목록 (읽기 전용)</summary>
        public IReadOnlyList<CardData> Cards => _cards;

        // -------------------------------------------------------
        // 유니티 생명주기
        // -------------------------------------------------------

        private void Start()
        {
            InitializeHand();
        }

        // -------------------------------------------------------
        // 초기화
        // -------------------------------------------------------

        /// <summary>
        /// 게임 시작 시 초기 손패를 구성한다.
        /// 기본 벽 카드 N장 + 기본 타워 카드 N장 + 추가 카드 목록 순으로 지급.
        /// </summary>
        private void InitializeHand()
        {
            for (int i = 0; i < _initialWallCardCount; i++)
            {
                if (_defaultWallCard != null)
                    AddCard(_defaultWallCard);
                else
                    Debug.LogWarning("[Hand] 기본 벽 카드가 연결되지 않았습니다.");
            }

            for (int i = 0; i < _initialTowerCardCount; i++)
            {
                if (_defaultTowerCard != null)
                    AddCard(_defaultTowerCard);
                else
                    Debug.LogWarning("[Hand] 기본 타워 카드가 연결되지 않았습니다.");
            }

            foreach (CardData card in _additionalStartingCards)
            {
                if (card != null) AddCard(card);
            }

            Debug.Log($"[Hand] 초기 손패 구성 완료: 총 {_cards.Count}장");
        }

        // -------------------------------------------------------
        // 카드 추가 / 제거
        // -------------------------------------------------------

        /// <summary>
        /// 손패에 카드를 추가한다.
        /// 같은 CardData 에셋을 여러 장 보유할 수 있다 (동일 참조 허용).
        /// </summary>
        public void AddCard(CardData card)
        {
            if (card == null) return;
            _cards.Add(card);
        }

        /// <summary>
        /// 손패에서 카드를 한 장 제거한다.
        /// 같은 종류의 카드가 여러 장이면 첫 번째 것만 제거한다.
        /// 반환값: 제거 성공 여부
        /// </summary>
        public bool RemoveCard(CardData card)
        {
            return _cards.Remove(card);
        }

        // -------------------------------------------------------
        // Inspector ContextMenu (디버그)
        // -------------------------------------------------------

        /// <summary>
        /// 코스트 획득 대신 카드를 즉시 손패에 추가하는 디버그 메서드.
        /// </summary>
        [ContextMenu("Debug: 기본 벽 카드 추가")]
        private void DebugAddWallCard()
        {
            if (_defaultWallCard != null) AddCard(_defaultWallCard);
        }

        [ContextMenu("Debug: 기본 타워 카드 추가")]
        private void DebugAddTowerCard()
        {
            if (_defaultTowerCard != null) AddCard(_defaultTowerCard);
        }

        [ContextMenu("Debug: 손패 초기화")]
        private void DebugClearHand()
        {
            _cards.Clear();
            Debug.Log("[Hand] 손패 초기화 완료.");
        }
    }
}
