using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace POC4
{
    /// <summary>
    /// 타워 배치를 제어하는 클래스.
    ///
    /// 배치 규칙:
    ///   - 벽 위에만 설치 가능
    ///   - 벽 하나당 타워 1개
    ///   - 좌클릭 시 즉시 설치 (확정 버튼 없음)
    ///
    /// 흐름:
    ///   1. Canvas 버튼에서 SelectArrow/Laser/CannonTower() 호출 → Placing 상태
    ///   2. 마우스가 유효한 벽 위에 오면 초록색 미리보기 표시
    ///   3. 좌클릭 → 즉시 설치
    ///   4. 우클릭 / CancelPlacing() → 선택 취소
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

        [Tooltip("LaserTower 컴포넌트가 붙은 프리팹")]
        [SerializeField] private LaserTower _laserTowerPrefab;

        [Tooltip("CannonTower 컴포넌트가 붙은 프리팹")]
        [SerializeField] private CannonTower _cannonTowerPrefab;

        [Header("Tower Data Assets")]
        [SerializeField] private TowerData _arrowTowerData;
        [SerializeField] private TowerData _laserTowerData;
        [SerializeField] private TowerData _cannonTowerData;

        [Header("Preview Visual")]
        [SerializeField] private Color _validPreviewColor = new Color(0f, 1f, 0f, 0.5f);
        [SerializeField] private Color _invalidPreviewColor = new Color(1f, 0f, 0f, 0.3f);

        // -------------------------------------------------------
        // 내부 상태
        // -------------------------------------------------------

        private bool _isPlacing;
        private TowerData _selectedData;

        /// <summary>현재 선택된 타워 프리팹. 추상 기반 타입으로 모든 타워 종류를 수용한다.</summary>
        private Tower _selectedPrefab;

        /// <summary>현재 마우스가 올라간 셀 좌표</summary>
        private Vector2Int _hoveredCell;

        /// <summary>현재 마우스가 올라간 셀을 포함하는 WallObject (해당 셀에 타워 없을 때만 유효)</summary>
        private WallObject _hoveredWall;

        // 미리보기용 SpriteRenderer (호버된 셀 위치에 표시)
        private SpriteRenderer _previewRenderer;

        // -------------------------------------------------------
        // 이벤트
        // -------------------------------------------------------

        /// <summary>
        /// 타워 설치가 완료될 때 발생한다.
        /// HandUI가 구독해 카드를 소비한다.
        /// </summary>
        public event Action OnTowerPlaced;

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
        /// Canvas UI 위 클릭은 무시한다.
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

            // 설치 완료 이벤트 발생 (HandUI가 카드 소비에 사용)
            OnTowerPlaced?.Invoke();

            // 선택 상태 유지: 연속으로 같은 종류 타워를 다른 셀에 설치 가능
        }

        // -------------------------------------------------------
        // 카드 시스템 연동 및 Canvas 버튼 콜백
        // -------------------------------------------------------

        /// <summary>
        /// HandUI에서 타워 카드를 선택했을 때 호출한다.
        /// TowerData의 TowerType에 맞는 프리팹을 자동으로 선택해 배치 모드를 시작한다.
        /// </summary>
        public void StartPlacingFromCard(TowerData data)
        {
            Tower prefab = GetPrefabForType(data.Type);
            if (prefab == null)
            {
                Debug.LogWarning($"[TowerPlacer] TowerType '{data.Type}'에 해당하는 프리팹이 연결되지 않았습니다.");
                return;
            }

            _selectedData = data;
            _selectedPrefab = prefab;
            _isPlacing = true;
        }

        /// <summary>
        /// 화살 타워 선택 Canvas 버튼 OnClick에 연결한다.
        /// </summary>
        public void SelectArrowTower()
        {
            if (_arrowTowerData != null && _arrowTowerPrefab != null)
                StartPlacingFromCard(_arrowTowerData);
        }

        /// <summary>
        /// 레이저 타워 선택 Canvas 버튼 OnClick에 연결한다.
        /// </summary>
        public void SelectLaserTower()
        {
            if (_laserTowerData != null && _laserTowerPrefab != null)
                StartPlacingFromCard(_laserTowerData);
        }

        /// <summary>
        /// 포탄 타워 선택 Canvas 버튼 OnClick에 연결한다.
        /// </summary>
        public void SelectCannonTower()
        {
            if (_cannonTowerData != null && _cannonTowerPrefab != null)
                StartPlacingFromCard(_cannonTowerData);
        }

        /// <summary>
        /// TowerType에 맞는 타워 프리팹을 반환한다.
        /// </summary>
        private Tower GetPrefabForType(TowerData.TowerType type)
        {
            return type switch
            {
                TowerData.TowerType.Arrow  => _arrowTowerPrefab,
                TowerData.TowerType.Laser  => _laserTowerPrefab,
                TowerData.TowerType.Cannon => _cannonTowerPrefab,
                _ => _arrowTowerPrefab
            };
        }

        /// <summary>
        /// 배치 모드를 취소하고 Idle 상태로 초기화한다.
        /// HandUI에서 다른 카드를 선택하거나 취소 시 호출한다.
        /// </summary>
        public void CancelPlacing()
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
        /// 마우스가 Canvas UI 위에 있는지 확인한다.
        /// Canvas EventSystem이 UI 레이캐스트를 처리하므로 IsPointerOverGameObject()로 판단한다.
        /// </summary>
        private bool IsMouseOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
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
