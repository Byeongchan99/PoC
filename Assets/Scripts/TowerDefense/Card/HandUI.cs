using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace POC4
{
    /// <summary>
    /// 손패 UI를 담당하는 클래스.
    /// 화면 하단에 보유 카드를 IMGUI로 표시하고,
    /// 카드 클릭 시 해당 설치 모드(WallPlacer / TowerPlacer)를 시작한다.
    ///
    /// 카드 소비 시점:
    ///   - 벽 카드: WallPlacer.OnWallPlaced 이벤트 수신 시 (설치 확정 후)
    ///   - 타워 카드: TowerPlacer.OnTowerPlaced 이벤트 수신 시 (즉시 설치 후)
    ///
    /// 대기 카드 추적 방식:
    ///   CardData 참조 대신 손패 내 인덱스로 추적한다.
    ///   같은 CardData 에셋이 여러 장 있을 때 클릭한 카드만 정확히 강조/소비된다.
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

        [Header("Card UI Settings")]
        [Tooltip("카드 버튼 하나의 너비 (픽셀)")]
        [SerializeField] private float _cardWidth = 100f;

        [Tooltip("카드 버튼 하나의 높이 (픽셀)")]
        [SerializeField] private float _cardHeight = 80f;

        [Tooltip("카드 사이 간격 (픽셀)")]
        [SerializeField] private float _cardSpacing = 8f;

        [Tooltip("화면 하단으로부터의 여백 (픽셀)")]
        [SerializeField] private float _bottomMargin = 10f;

        // -------------------------------------------------------
        // 내부 상태
        // -------------------------------------------------------

        /// <summary>
        /// 현재 설치 대기 중인 카드의 손패 인덱스.
        /// -1이면 대기 중인 카드 없음.
        /// CardData 참조 대신 인덱스를 사용해 같은 에셋 중복 보유 시에도 정확히 한 장만 추적한다.
        /// </summary>
        private int _pendingCardIndex = -1;

        /// <summary>손패 UI 전체 영역 (IsMouseOverUI 판단용)</summary>
        private Rect _handPanelRect;

        // -------------------------------------------------------
        // 유니티 생명주기
        // -------------------------------------------------------

        private void OnEnable()
        {
            // 설치 완료 이벤트 구독
            if (_wallPlacer != null) _wallPlacer.OnWallPlaced += HandleWallPlaced;
            if (_towerPlacer != null) _towerPlacer.OnTowerPlaced += HandleTowerPlaced;
        }

        private void OnDisable()
        {
            if (_wallPlacer != null) _wallPlacer.OnWallPlaced -= HandleWallPlaced;
            if (_towerPlacer != null) _towerPlacer.OnTowerPlaced -= HandleTowerPlaced;
        }

        // -------------------------------------------------------
        // 이벤트 핸들러 (카드 소비)
        // -------------------------------------------------------

        /// <summary>
        /// 벽 설치가 확정되면 대기 중인 카드를 손패에서 제거한다.
        /// </summary>
        private void HandleWallPlaced()
        {
            ConsumeCard();
        }

        /// <summary>
        /// 타워 설치가 완료되면 대기 중인 카드를 손패에서 제거한다.
        /// </summary>
        private void HandleTowerPlaced()
        {
            ConsumeCard();
        }

        /// <summary>
        /// 대기 중인 카드를 인덱스로 손패에서 제거한다.
        /// </summary>
        private void ConsumeCard()
        {
            if (_pendingCardIndex < 0) return;
            _hand.RemoveCardAt(_pendingCardIndex);
            _pendingCardIndex = -1;
        }

        // -------------------------------------------------------
        // IMGUI 렌더링
        // -------------------------------------------------------

        private void OnGUI()
        {
            if (_hand == null) return;

            IReadOnlyList<CardData> cards = _hand.Cards;
            int cardCount = cards.Count;

            // 손패 패널 크기 및 위치 계산 (화면 하단 중앙)
            float totalWidth = cardCount * _cardWidth + Mathf.Max(0, cardCount - 1) * _cardSpacing;
            float panelX = (Screen.width - totalWidth) * 0.5f;
            float panelY = Screen.height - _cardHeight - _bottomMargin;

            _handPanelRect = new Rect(panelX - 10f, panelY - 10f,
                                      totalWidth + 20f, _cardHeight + 20f);

            GUILayout.BeginArea(_handPanelRect);
            GUILayout.BeginHorizontal();

            for (int i = 0; i < cardCount; i++)
            {
                DrawCard(cards[i], i);
                if (i < cardCount - 1)
                    GUILayout.Space(_cardSpacing);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            // 카드가 없을 때 안내 문구
            if (cardCount == 0)
            {
                GUI.Label(new Rect(Screen.width * 0.5f - 80f, Screen.height - 40f, 160f, 30f),
                          "[ 손패가 비어 있습니다 ]");
            }

            // 대기 중인 카드 상태 표시
            if (_pendingCardIndex >= 0 && _pendingCardIndex < cards.Count)
            {
                CardData pendingCard = cards[_pendingCardIndex];
                string label = pendingCard.Kind == CardData.CardKind.Wall
                    ? "벽 배치 중 - 그리드에 위치 선택 후 확정"
                    : "타워 배치 중 - 벽 셀 위를 클릭";
                GUI.Label(new Rect(Screen.width * 0.5f - 150f, Screen.height - _cardHeight - 50f,
                                   300f, 30f), label);
            }
        }

        /// <summary>
        /// 카드 한 장을 버튼으로 그린다.
        /// 대기 중인 카드 인덱스와 일치하면 강조 표시한다.
        /// </summary>
        private void DrawCard(CardData card, int index)
        {
            bool isPending = index == _pendingCardIndex;

            // 대기 중인 카드는 배경색 변경으로 강조
            Color prevColor = GUI.backgroundColor;
            if (isPending) GUI.backgroundColor = new Color(1f, 1f, 0.4f);

            string label = BuildCardLabel(card);
            bool clicked = GUILayout.Button(label,
                GUILayout.Width(_cardWidth), GUILayout.Height(_cardHeight));

            GUI.backgroundColor = prevColor;

            if (clicked && !isPending)
            {
                UseCard(card, index);
            }
        }

        /// <summary>
        /// 카드 버튼에 표시할 텍스트를 생성한다.
        /// </summary>
        private string BuildCardLabel(CardData card)
        {
            if (card.Kind == CardData.CardKind.Wall && card.WallData != null)
            {
                string effect = card.WallData.EffectType == WallData.WallEffectType.None
                    ? "" : $"\n[{card.WallData.EffectType}]";
                return $"벽\n{card.WallData.Type}{effect}";
            }

            if (card.Kind == CardData.CardKind.Tower && card.TowerData != null)
            {
                string effect = card.TowerData.EffectType == TowerData.TowerEffectType.None
                    ? "" : $"\n[{card.TowerData.EffectType}]";
                return $"타워\n{card.TowerData.Type}{effect}";
            }

            return card.DisplayName;
        }

        // -------------------------------------------------------
        // 카드 사용
        // -------------------------------------------------------

        /// <summary>
        /// 카드를 클릭했을 때 호출한다.
        /// 이미 다른 카드를 사용 중이면 기존 설치 모드를 취소하고 새 카드를 선택한다.
        /// </summary>
        private void UseCard(CardData card, int index)
        {
            if (!card.IsValid())
            {
                Debug.LogWarning($"[HandUI] 카드 데이터가 올바르지 않습니다: {card.name}");
                return;
            }

            // 기존 설치 취소
            CancelCurrentPlacing();

            _pendingCardIndex = index;

            if (card.Kind == CardData.CardKind.Wall)
            {
                StartWallPlacing(card.WallData);
            }
            else
            {
                StartTowerPlacing(card.TowerData);
            }
        }

        /// <summary>
        /// WallPlacer에 벽 카드의 WallData를 전달해 배치 모드를 시작한다.
        /// </summary>
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

        /// <summary>
        /// TowerPlacer에 타워 카드의 TowerData를 전달해 배치 모드를 시작한다.
        /// </summary>
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

        /// <summary>
        /// 현재 진행 중인 배치를 취소한다.
        /// 대기 중인 카드는 소비하지 않고 손패에 그대로 남긴다.
        /// </summary>
        private void CancelCurrentPlacing()
        {
            if (_pendingCardIndex < 0) return;

            _wallPlacer?.Cancel();
            _towerPlacer?.CancelPlacing();
            _pendingCardIndex = -1;
        }

        // -------------------------------------------------------
        // UI 영역 판단 (외부 참조용)
        // -------------------------------------------------------

        /// <summary>
        /// 마우스가 손패 UI 영역 위에 있는지 반환한다.
        /// WallPlacer, TowerPlacer가 월드 클릭 처리 시 UI 영역을 제외하기 위해 사용.
        /// Input System 좌표(좌측 하단 기준)를 GUI 좌표(좌측 상단 기준)로 변환한다.
        /// </summary>
        public bool IsMouseOverHandUI
        {
            get
            {
                Vector2 mouse = Mouse.current.position.ReadValue();
                Vector2 guiMouse = new Vector2(mouse.x, Screen.height - mouse.y);
                return _handPanelRect.Contains(guiMouse);
            }
        }
    }
}
