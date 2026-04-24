using UnityEngine;
using UnityEngine.InputSystem;

namespace POC4
{
    /// <summary>
    /// 타워 배치를 제어하는 클래스.
    ///
    /// 배치 규칙:
    ///   - 벽 위에만 설치 가능
    ///   - 벽 하나당 타워 1개
    ///   - 드롭 시 즉시 설치 (확정 버튼 없음)
    ///
    /// 흐름:
    ///   1. OnGUI 버튼으로 타워 종류 선택 → Placing 상태
    ///   2. 마우스가 유효한 벽 위에 오면 초록색 미리보기 표시
    ///   3. 좌클릭 → 즉시 설치
    ///   4. 우클릭 / 취소 버튼 → 선택 취소
    /// </summary>
    public class TowerPlacer : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector 노출 필드
        // -------------------------------------------------------

        [Header("References")]
        [SerializeField] private GridSystem _gridSystem;

        [Header("Tower Prefabs (각 종류별 프리팹 연결)")]
        [Tooltip("ArrowTower 컴포넌트가 붙은 프리팹")]
        [SerializeField] private ArrowTower _arrowTowerPrefab;

        [Header("Tower Data Assets")]
        [SerializeField] private TowerData _arrowTowerData;

        [Header("Preview Visual")]
        [SerializeField] private Color _validPreviewColor = new Color(0f, 1f, 0f, 0.5f);
        [SerializeField] private Color _invalidPreviewColor = new Color(1f, 0f, 0f, 0.3f);

        // -------------------------------------------------------
        // 내부 상태
        // -------------------------------------------------------

        private bool _isPlacing;
        private TowerData _selectedData;
        private ArrowTower _selectedPrefab;

        /// <summary>현재 마우스가 올라간 셀 좌표</summary>
        private Vector2Int _hoveredCell;

        /// <summary>현재 마우스가 올라간 셀을 포함하는 WallObject (해당 셀에 타워 없을 때만 유효)</summary>
        private WallObject _hoveredWall;

        // 미리보기용 SpriteRenderer (호버된 셀 위치에 표시)
        private SpriteRenderer _previewRenderer;

        // OnGUI UI 영역 (WallPlacer의 영역과 겹치지 않도록 오른쪽에 배치)
        private readonly Rect _uiRect = new Rect(Screen.width - 200, 10, 190, 200);

        // -------------------------------------------------------
        // 유니티 생명주기
        // -------------------------------------------------------

        private void Awake()
        {
            CreatePreviewRenderer();
        }

        private void Update()
        {
            if (!_isPlacing) return;

            UpdateHoveredWall();
            UpdatePreview();
            HandleRightClick();
            HandleLeftClick();
        }

        // -------------------------------------------------------
        // 미리보기 렌더러 초기화
        // -------------------------------------------------------

        /// <summary>
        /// 타워 배치 위치를 표시할 SpriteRenderer를 생성한다.
        /// </summary>
        private void CreatePreviewRenderer()
        {
            GameObject previewObj = new GameObject("TowerPreview");
            previewObj.transform.SetParent(transform);

            _previewRenderer = previewObj.AddComponent<SpriteRenderer>();
            _previewRenderer.sprite = CreateWhiteSprite();
            _previewRenderer.sortingOrder = 10;
            _previewRenderer.gameObject.SetActive(false);
        }

        // -------------------------------------------------------
        // 마우스 추적 및 벽 탐색
        // -------------------------------------------------------

        /// <summary>
        /// 마우스 위치를 그리드 좌표로 변환하고, 해당 셀을 포함하는 WallObject를 탐색한다.
        /// 해당 셀에 이미 타워가 있으면 null로 처리한다.
        /// </summary>
        private void UpdateHoveredWall()
        {
            Vector2 mouseScreen = Mouse.current.position.ReadValue();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(
                new Vector3(mouseScreen.x, mouseScreen.y, 0f));
            worldPos.z = 0f;

            _hoveredCell = _gridSystem.WorldToGridPosition(worldPos);
            _hoveredWall = FindWallAtCell(_hoveredCell);
        }

        /// <summary>
        /// 씬의 모든 WallObject 중 지정한 셀을 점유하고 있고,
        /// 해당 셀에 아직 타워가 없는 것을 반환한다.
        /// 셀 단위로 판정하므로 같은 벽의 다른 셀에는 여전히 설치 가능.
        /// </summary>
        private WallObject FindWallAtCell(Vector2Int cell)
        {
            WallObject[] walls = FindObjectsByType<WallObject>(FindObjectsSortMode.None);
            foreach (WallObject wall in walls)
            {
                foreach (Vector2Int occupied in wall.OccupiedCells)
                {
                    if (occupied == cell && !wall.HasTowerAtCell(cell))
                        return wall;
                }
            }
            return null;
        }

        // -------------------------------------------------------
        // 미리보기 갱신
        // -------------------------------------------------------

        /// <summary>
        /// 유효한 벽 셀 위에 있으면 초록색 미리보기를, 없으면 미리보기를 숨긴다.
        /// 미리보기는 벽 중심이 아닌 마우스가 올라간 셀 위치에 표시한다.
        /// </summary>
        private void UpdatePreview()
        {
            if (_hoveredWall == null)
            {
                _previewRenderer.gameObject.SetActive(false);
                return;
            }

            float size = _gridSystem.CellSize * 0.7f;

            _previewRenderer.gameObject.SetActive(true);
            _previewRenderer.transform.position = _gridSystem.GridToWorldPosition(_hoveredCell);
            _previewRenderer.transform.localScale = Vector3.one * size;
            _previewRenderer.color = _validPreviewColor;
        }

        // -------------------------------------------------------
        // 입력 처리
        // -------------------------------------------------------

        /// <summary>
        /// 우클릭: 타워 선택을 취소하고 Idle 상태로 복귀한다.
        /// </summary>
        private void HandleRightClick()
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                CancelPlacing();
            }
        }

        /// <summary>
        /// 좌클릭: 유효한 벽 위에 타워를 즉시 설치한다.
        /// UI 영역 클릭은 무시한다.
        /// </summary>
        private void HandleLeftClick()
        {
            if (!Mouse.current.leftButton.wasPressedThisFrame) return;
            if (IsMouseOverUI()) return;
            if (_hoveredWall == null) return;

            PlaceTower(_hoveredWall);
        }

        // -------------------------------------------------------
        // 타워 설치
        // -------------------------------------------------------

        /// <summary>
        /// 호버된 셀 위치에 타워를 즉시 설치한다.
        /// 타워는 셀 중심에 배치되며, 해당 셀만 점유로 표시된다.
        /// 설치 후 선택 상태를 유지해 연속 설치가 가능하다.
        /// </summary>
        private void PlaceTower(WallObject wall)
        {
            Vector3 spawnPos = _gridSystem.GridToWorldPosition(_hoveredCell);
            Tower tower = Instantiate(_selectedPrefab, spawnPos, Quaternion.identity);

            // 타워 스탯 초기화
            tower.Initialize(_selectedData);

            // 벽 효과 적용 (벽에 효과가 있으면 타워 스탯에 보너스 추가)
            tower.ApplyWallBonus(wall.WallData);

            // 해당 셀에 타워 설치 완료 표시 (셀 단위)
            wall.SetTowerAtCell(_hoveredCell);

            // 선택 상태 유지: 연속으로 같은 종류 타워를 다른 셀에 설치 가능
        }

        // -------------------------------------------------------
        // 상태 관리
        // -------------------------------------------------------

        /// <summary>
        /// 타워 선택을 취소하고 Idle 상태로 초기화한다.
        /// </summary>
        private void CancelPlacing()
        {
            _isPlacing = false;
            _selectedData = null;
            _selectedPrefab = null;
            _hoveredWall = null;
            _previewRenderer.gameObject.SetActive(false);
        }

        // -------------------------------------------------------
        // UI 영역 판단
        // -------------------------------------------------------

        /// <summary>
        /// 마우스가 OnGUI 영역 위에 있는지 확인한다.
        /// Input System의 좌표는 좌측 하단 기준이므로 y 반전.
        /// </summary>
        private bool IsMouseOverUI()
        {
            Vector2 mouseScreen = Mouse.current.position.ReadValue();
            Vector2 guiMouse = new Vector2(mouseScreen.x, Screen.height - mouseScreen.y);
            return _uiRect.Contains(guiMouse);
        }

        // -------------------------------------------------------
        // OnGUI 테스트 팔레트
        // -------------------------------------------------------

        private void OnGUI()
        {
            // Screen.width는 런타임에만 유효하므로 Rect를 매 OnGUI마다 갱신
            Rect rect = new Rect(Screen.width - 200, 10, 190, 200);

            GUILayout.BeginArea(rect);
            GUILayout.Label("[ 타워 선택 ]");
            GUILayout.Space(4);

            if (_isPlacing)
            {
                GUILayout.Label($"배치 중: {_selectedData.name}");
                GUILayout.Label("벽 위 좌클릭: 즉시 설치");
                GUILayout.Label("우클릭 / 취소: 선택 해제");
                GUILayout.Space(4);
                if (GUILayout.Button("취소"))
                {
                    CancelPlacing();
                }
            }
            else
            {
                DrawTowerButton("화살 타워", _arrowTowerData, _arrowTowerPrefab);
                GUILayout.Space(8);
                GUILayout.Label("벽 위에 올리면 초록 미리보기");
                GUILayout.Label("좌클릭으로 즉시 설치");
            }

            GUILayout.EndArea();
        }

        /// <summary>
        /// 타워 선택 버튼을 그린다. data 또는 prefab이 null이면 비활성화.
        /// </summary>
        private void DrawTowerButton(string label, TowerData data, ArrowTower prefab)
        {
            GUI.enabled = data != null && prefab != null;
            if (GUILayout.Button(label) && data != null && prefab != null)
            {
                _selectedData = data;
                _selectedPrefab = prefab;
                _isPlacing = true;
            }
            GUI.enabled = true;
        }

        // -------------------------------------------------------
        // 스프라이트 생성
        // -------------------------------------------------------

        private Sprite CreateWhiteSprite()
        {
            const int size = 4;
            Texture2D tex = new Texture2D(size, size) { filterMode = FilterMode.Point };
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        // -------------------------------------------------------
        // Inspector ContextMenu (디버그)
        // -------------------------------------------------------

        [ContextMenu("Debug: 모든 타워 제거")]
        private void DebugRemoveAllTowers()
        {
            Tower[] towers = FindObjectsByType<Tower>(FindObjectsSortMode.None);
            foreach (Tower t in towers) Destroy(t.gameObject);
            Debug.Log($"[TowerPlacer] 타워 {towers.Length}개 제거.");
        }
    }
}
