using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace POC4
{
    /// <summary>
    /// 벽 배치 관련 UI를 담당하는 클래스.
    ///
    /// WallPlacer의 상태에 따라 Canvas 패널을 전환한다.
    ///   - Idle 상태: 벽 종류 선택 팔레트 패널 표시
    ///   - Placing / Dropped 상태: 배치 안내 패널 표시
    ///
    /// _placingPanel은 미리보기 오른쪽에 따라다닌다.
    /// Canvas 버튼에서 SelectWall*() 메서드를 Inspector에 직접 연결한다.
    /// </summary>
    public class WallPlacementUI : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector 노출 필드
        // -------------------------------------------------------

        [Header("References")]
        [SerializeField] private WallPlacer _wallPlacer;

        [Header("Wall Data Assets (각 종류별 ScriptableObject 연결)")]
        [SerializeField] private WallData _wallDataI;
        [SerializeField] private WallData _wallDataO;
        [SerializeField] private WallData _wallDataT;
        [SerializeField] private WallData _wallDataS;
        [SerializeField] private WallData _wallDataZ;
        [SerializeField] private WallData _wallDataL;
        [SerializeField] private WallData _wallDataJ;

        [Header("Canvas Panels")]
        [Tooltip("Idle 상태에서 표시할 벽 종류 선택 팔레트 패널")]
        [SerializeField] private GameObject _palettePanel;

        [Tooltip("Placing / Dropped 상태에서 표시할 배치 안내 패널 (미리보기 오른쪽에 따라다님)")]
        [SerializeField] private GameObject _placingPanel;

        [Header("Placing Panel UI")]
        [Tooltip("현재 배치 상태 설명 텍스트")]
        [SerializeField] private TMP_Text _stateDescriptionText;

        [Tooltip("설치 확정 버튼 (Dropped이고 유효할 때만 활성화)")]
        [SerializeField] private Button _confirmButton;

        [Header("Placing Panel Follow Settings")]
        [Tooltip("미리보기 위치 기준으로 패널을 이동시킬 Canvas (없으면 부모에서 자동 탐색)")]
        [SerializeField] private Canvas _canvas;

        [Tooltip("미리보기 스크린 좌표 기준 패널 오프셋 (픽셀 단위). 오른쪽으로 이동하려면 x 양수.")]
        [SerializeField] private Vector2 _placingPanelOffset = new Vector2(60f, 0f);

        // -------------------------------------------------------
        // 내부 상태
        // -------------------------------------------------------

        private RectTransform _placingPanelRT;

        // -------------------------------------------------------
        // 유니티 생명주기
        // -------------------------------------------------------

        private void Awake()
        {
            if (_canvas == null)
                _canvas = GetComponentInParent<Canvas>();

            if (_placingPanel != null)
                _placingPanelRT = _placingPanel.GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (_wallPlacer == null) return;

            bool isIdle = _wallPlacer.State == WallPlacer.PlacerState.Idle;
            _palettePanel?.SetActive(isIdle);
            _placingPanel?.SetActive(!isIdle);

            if (!isIdle)
            {
                UpdatePlacingUI();
                UpdatePlacingPanelPosition();
            }
        }

        // -------------------------------------------------------
        // 배치 상태 UI 갱신
        // -------------------------------------------------------

        /// <summary>
        /// 배치 상태에 따라 안내 텍스트와 확정 버튼 활성화 여부를 갱신한다.
        /// </summary>
        private void UpdatePlacingUI()
        {
            if (_stateDescriptionText != null)
            {
                if (_wallPlacer.State == WallPlacer.PlacerState.Placing)
                {
                    _stateDescriptionText.text =
                        "초록: 드롭 가능\n빨강: 겹침 / 범위 초과\n우클릭: 회전  /  좌클릭: 드롭";
                }
                else
                {
                    _stateDescriptionText.text = _wallPlacer.IsCurrentValid
                        ? "설치 가능 (경로 열림)\n좌클릭: 다시 들어올리기"
                        : "경로 차단 - 위치 조정 필요\n좌클릭: 다시 들어올리기";
                }
            }

            if (_confirmButton != null)
            {
                _confirmButton.interactable =
                    _wallPlacer.State == WallPlacer.PlacerState.Dropped && _wallPlacer.IsCurrentValid;
            }
        }

        // -------------------------------------------------------
        // 패널 위치 추적
        // -------------------------------------------------------

        /// <summary>
        /// 미리보기의 월드 좌표를 스크린 좌표로 변환한 뒤 오프셋을 더해 패널을 이동시킨다.
        /// Canvas가 Screen Space - Overlay 모드일 때 카메라 파라미터에 null을 전달한다.
        /// </summary>
        private void UpdatePlacingPanelPosition()
        {
            if (_placingPanelRT == null) return;
            if (_canvas == null) return;
            if (!_wallPlacer.HasPreviewPosition) return;

            // 미리보기 월드 좌표 → 스크린 좌표
            Vector3 worldPos = _wallPlacer.CurrentPreviewWorldPosition;
            Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            // 오른쪽으로 오프셋 적용
            screenPos += _placingPanelOffset;

            // 스크린 좌표 → Canvas 로컬 좌표
            Camera uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _canvas.worldCamera;

            RectTransform canvasRT = _canvas.GetComponent<RectTransform>();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRT, screenPos, uiCamera, out Vector2 localPoint))
            {
                _placingPanelRT.anchoredPosition = localPoint;
            }
        }

        // -------------------------------------------------------
        // 벽 선택 (각 Canvas 버튼 OnClick에 연결)
        // -------------------------------------------------------

        public void SelectWallI() => _wallPlacer?.StartPlacing(_wallDataI);
        public void SelectWallO() => _wallPlacer?.StartPlacing(_wallDataO);
        public void SelectWallT() => _wallPlacer?.StartPlacing(_wallDataT);
        public void SelectWallS() => _wallPlacer?.StartPlacing(_wallDataS);
        public void SelectWallZ() => _wallPlacer?.StartPlacing(_wallDataZ);
        public void SelectWallL() => _wallPlacer?.StartPlacing(_wallDataL);
        public void SelectWallJ() => _wallPlacer?.StartPlacing(_wallDataJ);

        /// <summary>설치 확정 버튼 OnClick에 연결한다.</summary>
        public void Confirm() => _wallPlacer?.Confirm();

        /// <summary>취소 버튼 OnClick에 연결한다.</summary>
        public void Cancel() => _wallPlacer?.Cancel();

        // -------------------------------------------------------
        // UI 영역 판단
        // -------------------------------------------------------

        /// <summary>
        /// 마우스가 Canvas UI 위에 있는지 여부.
        /// WallPlacer가 월드 클릭과 UI 클릭을 구분하기 위해 참조한다.
        /// </summary>
        public bool IsMouseOverUI =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
