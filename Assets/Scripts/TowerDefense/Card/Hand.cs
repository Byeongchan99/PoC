using System.Collections.Generic;
using UnityEngine;

namespace POC4
{
    /// <summary>
    /// 플레이어의 손패(보유 카드 목록)를 관리하는 클래스.
    /// 게임 시작 시 벽/타워 카드 풀에서 중복 없이 랜덤 추출해 초기 손패를 구성한다.
    /// POC에서 손패 최대 크기는 무제한.
    /// </summary>
    public class Hand : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector 노출 필드
        // -------------------------------------------------------

        [Header("Wall Card Pool (시작 손패 랜덤 추출 풀)")]
        [Tooltip("벽 카드 7종 에셋을 모두 등록한다. 이 중 _initialWallCardCount장을 중복 없이 랜덤 지급.")]
        [SerializeField] private List<CardData> _wallCardPool = new List<CardData>();

        [Tooltip("시작 손패에 지급할 벽 카드 수 (풀 크기 초과 시 풀 전체 지급)")]
        [SerializeField] private int _initialWallCardCount = 3;

        [Header("Tower Card Pool (시작 손패 랜덤 추출 풀)")]
        [Tooltip("타워 카드 3종 에셋을 모두 등록한다. 이 중 _initialTowerCardCount장을 중복 없이 랜덤 지급.")]
        [SerializeField] private List<CardData> _towerCardPool = new List<CardData>();

        [Tooltip("시작 손패에 지급할 타워 카드 수 (풀 크기 초과 시 풀 전체 지급)")]
        [SerializeField] private int _initialTowerCardCount = 3;

        [Header("Additional Starting Cards (선택, 위 풀 외 추가 지급)")]
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
        /// 벽 카드 풀과 타워 카드 풀에서 각각 중복 없이 랜덤 추출 후 추가 카드를 지급한다.
        /// </summary>
        private void InitializeHand()
        {
            List<CardData> wallSampled = SampleWithoutReplacement(_wallCardPool, _initialWallCardCount);
            foreach (CardData card in wallSampled)
                AddCard(card);

            List<CardData> towerSampled = SampleWithoutReplacement(_towerCardPool, _initialTowerCardCount);
            foreach (CardData card in towerSampled)
                AddCard(card);

            foreach (CardData card in _additionalStartingCards)
            {
                if (card != null) AddCard(card);
            }

            Debug.Log($"[Hand] 초기 손패 구성 완료: 벽 {wallSampled.Count}장 + 타워 {towerSampled.Count}장 = 총 {_cards.Count}장");
        }

        /// <summary>
        /// 리스트에서 count장을 중복 없이 랜덤 추출한다. (Fisher-Yates 셔플)
        /// count가 리스트 크기보다 크면 전체를 반환한다.
        /// </summary>
        private List<CardData> SampleWithoutReplacement(List<CardData> pool, int count)
        {
            // null 항목 제거 후 복사본 생성
            List<CardData> shuffled = new List<CardData>();
            foreach (CardData card in pool)
            {
                if (card != null) shuffled.Add(card);
            }

            // Fisher-Yates 셔플: 뒤에서부터 랜덤 위치와 교환
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            // 요청 수만큼 잘라서 반환 (풀 크기 초과 시 전체 반환)
            int take = Mathf.Min(count, shuffled.Count);
            return shuffled.GetRange(0, take);
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
        /// 지정한 인덱스의 카드를 손패에서 제거한다.
        /// HandUI가 카드 소비 시 사용한다 (인덱스 기반이므로 같은 에셋 중복 보유 시에도 정확히 한 장만 제거).
        /// 반환값: 제거 성공 여부
        /// </summary>
        public bool RemoveCardAt(int index)
        {
            if (index < 0 || index >= _cards.Count) return false;
            _cards.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// 손패에서 동일 참조의 카드를 첫 번째 것만 제거한다.
        /// 외부 시스템에서 참조로 제거해야 할 때 사용한다.
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
        /// 벽 카드 풀에서 랜덤으로 한 장을 손패에 추가하는 디버그 메서드.
        /// </summary>
        [ContextMenu("Debug: 벽 카드 1장 추가 (랜덤)")]
        private void DebugAddWallCard()
        {
            List<CardData> sampled = SampleWithoutReplacement(_wallCardPool, 1);
            if (sampled.Count > 0) AddCard(sampled[0]);
        }

        /// <summary>
        /// 타워 카드 풀에서 랜덤으로 한 장을 손패에 추가하는 디버그 메서드.
        /// </summary>
        [ContextMenu("Debug: 타워 카드 1장 추가 (랜덤)")]
        private void DebugAddTowerCard()
        {
            List<CardData> sampled = SampleWithoutReplacement(_towerCardPool, 1);
            if (sampled.Count > 0) AddCard(sampled[0]);
        }

        [ContextMenu("Debug: 손패 초기화")]
        private void DebugClearHand()
        {
            _cards.Clear();
            Debug.Log("[Hand] 손패 초기화 완료.");
        }
    }
}
