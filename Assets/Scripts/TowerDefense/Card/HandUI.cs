using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace POC4
{
    /// <summary>
    /// 손패 UI를 담당하는 클래스.
    /// 손패 카드 수가 변경될 때마다 CardUI 프리팹을 인스턴스화/제거해 카드 목록을 갱신한다.
    ///
    /// 카드 소비 시점:
    ///   - 벽 카드: WallPlacer.OnWallPlaced 이벤트 수신 시 (설치 확정 후)
    ///   - 타워 카드: TowerPlacer.OnTowerPlaced 이벤트 수신 시 (즉시 설치 후)
    ///
    /// 대기 카드 추적:
    ///   CardData 참조 대신 손패 인덱스로 추적한다.
    ///   같은 CardData 에셋 여러 장 보유 시에도 클릭한 카드만 정확히 강조/소비된다.
    /// </summary>
    public class HandUI : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector 노출 필드
        // -------------------------------------------------------

        [Header("References")]
        [SerializeField] private Hand _hand;
        [SerializeField] private WallPlacer _wallPlacer;
        [SerializeField] private TowerPlacer _towerPlacer;

        [Header("Card UI")]
        [Tooltip("카드 한 장을 표시할 프리팹 (CardUI 컴포넌트 포함 필수)")]
        [SerializeField] private CardUI _cardUIPrefab;

        [Tooltip("카드 UI가 생성될 부모 Transform (Horizontal Layout Group 권장)")]
        [SerializeField] private Transform _cardContainer;

        [Tooltip("손패가 비었을 때 표시할 오브젝트 (예: '손패가 비어 있습니다' 텍스트)")]
        [SerializeField] private GameObject _emptyHandLabel;

        [Tooltip("배치 중인 카드의 상태 메시지를 표시하는 TMP_Text")]
        [SerializeField] private TMP_Text _placingStatusText;

        // -------------------------------------------------------
        // 내부 상태
        // -------------------------------------------------------

        /// <summary>
        /// 현재 설치 대기 중인 카드의 손패 인덱스.
        /// -1이면 대기 중인 카드 없음.
        /// </summary>
        private int _pendingCardIndex = -1;

        /// <summary>현재 씬에 생성된 CardUI 인스턴스 목록</summary>
        private readonly List<CardUI> _cardUIs = new List<CardUI>();

        /// <summary>직전 프레임의 손패 카드 수. 변화 감지에 사용.</summary>
        private int _lastHandCount = -1;

        // -------------------------------------------------------
        // 유니티 생명주기
        // -------------------------------------------------------

        private void OnEnable()
        {
            if (_wallPlacer != null) _wallPlacer.OnWallPlaced += HandleWallPlaced;
            if (_towerPlacer != null) _towerPlacer.OnTowerPlaced += HandleTowerPlaced;
        }

        private void OnDisable()
        {
            if (_wallPlacer != null) _wallPlacer.OnWallPlaced -= HandleWallPlaced;
            if (_towerPlacer != null) _towerPlacer.OnTowerPlaced -= HandleTowerPlaced;
        }

        private void Update()
        {
            if (_hand == null) return;

            int count = _hand.Cards.Count;

            // 손패 수가 변경된 경우에만 카드 UI를 재구성한다.
            if (count != _lastHandCount)
            {
                RebuildCardUIs();
                _lastHandCount = count;
            }

            UpdateEmptyLabel(count);
            UpdateStatusText();
        }

        // -------------------------------------------------------
        // 카드 UI 재구성
        // -------------------------------------------------------

        /// <summary>
        /// 기존 CardUI 인스턴스를 모두 제거하고 현재 손패 기준으로 새로 생성한다.
        /// 손패 수가 변경될 때마다 호출된다.
        /// </summary>
        private void RebuildCardUIs()
        {
            foreach (CardUI ui in _cardUIs)
            {
                if (ui != null) Destroy(ui.gameObject);
            }
            _cardUIs.Clear();

            if (_cardUIPrefab == null || _cardContainer == null) return;

            IReadOnlyList<CardData> cards = _hand.Cards;
            for (int i = 0; i < cards.Count; i++)
            {
                CardUI ui = Instantiate(_cardUIPrefab, _cardContainer);
                ui.Setup(cards[i], i, i == _pendingCardIndex, HandleCardClicked);
                _cardUIs.Add(ui);
            }
        }

        // -------------------------------------------------------
        // 카드 클릭 처리
        // -------------------------------------------------------

        /// <summary>
        /// CardUI 버튼 클릭 시 콜백으로 호출된다.
        /// 이미 선택된 카드를 다시 클릭하면 취소, 아닌 경우 새로 선택하고 배치 모드를 시작한다.
        /// </summary>
        private void HandleCardClicked(int index)
        {
            IReadOnlyList<CardData> cards = _hand.Cards;
            if (index < 0 || index >= cards.Count) return;

            CardData card = cards[index];
            if (!card.IsValid())
            {
                Debug.LogWarning("[HandUI] 카드 데이터가 올바르지 않습니다.");
                return;
            }

            // 이미 선택된 카드 재클릭 시 취소한다.
            if (index == _pendingCardIndex)
            {
                CancelCurrentPlacing();
                return;
            }

            // 기존 선택을 취소하고 새 카드를 선택한다.
            CancelCurrentPlacing();
            _pendingCardIndex = index;
            UpdateCardHighlights();

            if (card.Kind == CardData.CardKind.Wall)
                StartWallPlacing(card.WallData);
            else
                StartTowerPlacing(card.TowerData);
        }

        // -------------------------------------------------------
        // 카드 소비
        // -------------------------------------------------------

        private void HandleWallPlaced() => ConsumeCard();
        private void HandleTowerPlaced() => ConsumeCard();

        /// <summary>
        /// 대기 중인 카드를 손패에서 제거한다.
        /// Update()에서 손패 수 변화를 감지해 자동으로 UI를 재구성한다.
        /// </summary>
        private void ConsumeCard()
        {
            if (_pendingCardIndex < 0) return;
            _hand.RemoveCardAt(_pendingCardIndex);
            _pendingCardIndex = -1;
        }

        // -------------------------------------------------------
        // 배치 취소
        // -------------------------------------------------------

        /// <summary>
        /// 현재 진행 중인 배치를 취소한다.
        /// 카드는 소비하지 않고 손패에 유지된다.
        /// </summary>
        private void CancelCurrentPlacing()
        {
            if (_pendingCardIndex < 0) return;
            _wallPlacer?.Cancel();
            _towerPlacer?.CancelPlacing();
            _pendingCardIndex = -1;
            UpdateCardHighlights();
        }

        // -------------------------------------------------------
        // 배치 시작
        // -------------------------------------------------------

        private void StartWallPlacing(WallData wallData)
        {
            if (_wallPlacer == null)
            {
                Debug.LogError("[HandUI] WallPlacer가 연결되지 않았습니다.");
                _pendingCardIndex = -1;
                return;
            }
            _wallPlacer.StartPlacing(wallData);
        }

        private void StartTowerPlacing(TowerData towerData)
        {
            if (_towerPlacer == null)
            {
                Debug.LogError("[HandUI] TowerPlacer가 연결되지 않았습니다.");
                _pendingCardIndex = -1;
                return;
            }
            _towerPlacer.StartPlacingFromCard(towerData);
        }

        // -------------------------------------------------------
        // UI 갱신
        // -------------------------------------------------------

        /// <summary>
        /// 모든 CardUI의 강조 상태를 _pendingCardIndex 기준으로 갱신한다.
        /// </summary>
        private void UpdateCardHighlights()
        {
            for (int i = 0; i < _cardUIs.Count; i++)
            {
                if (_cardUIs[i] != null)
                    _cardUIs[i].SetHighlight(i == _pendingCardIndex);
            }
        }

        private void UpdateEmptyLabel(int cardCount)
        {
            if (_emptyHandLabel != null)
                _emptyHandLabel.SetActive(cardCount == 0);
        }

        /// <summary>
        /// 배치 중일 때 상태 텍스트를 표시하고, 아닐 때는 숨긴다.
        /// </summary>
        private void UpdateStatusText()
        {
            if (_placingStatusText == null) return;

            IReadOnlyList<CardData> cards = _hand.Cards;
            if (_pendingCardIndex >= 0 && _pendingCardIndex < cards.Count)
            {
                CardData card = cards[_pendingCardIndex];
                _placingStatusText.text = card.Kind == CardData.CardKind.Wall
                    ? "벽 배치 중 - 그리드에 위치 선택 후 확정"
                    : "타워 배치 중 - 벽 셀 위를 클릭";
                _placingStatusText.gameObject.SetActive(true);
            }
            else
            {
                _placingStatusText.gameObject.SetActive(false);
            }
        }

        // -------------------------------------------------------
        // UI 영역 판단
        // -------------------------------------------------------

        /// <summary>
        /// 마우스가 Canvas UI 위에 있는지 여부.
        /// Canvas EventSystem이 UI 레이캐스트를 처리하므로 IsPointerOverGameObject()로 판단한다.
        /// </summary>
        public bool IsMouseOverHandUI =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
