using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace POC4
{
    /// <summary>
    /// 벽 배치의 전체 흐름을 제어하는 클래스.
    ///
    /// 상태 전이:
    ///   Idle → (StartPlacing 호출) → Placing
    ///   Placing → (좌클릭, 겹침 없음) → Dropped   (A* 실행, 경로 검증)
    ///   Dropped → (좌클릭) → Placing               (다시 들어올리기)
    ///   Dropped → (Confirm, 유효할 때) → Idle      (벽 확정 배치)
    ///   Placing/Dropped → (우클릭) → Placing       (회전 후 Placing으로 복귀)
    ///   Placing/Dropped → (Cancel) → Idle
    ///
    /// 성능 고려:
    ///   - Placing 상태에서는 겹침/범위 체크만 수행 (A* 없음)
    ///   - A*는 드롭(좌클릭) 시점에 1회만 실행
    /// </summary>
    public class WallPlacer : MonoBehaviour
    {
        // -------------------------------------------------------
        // 상태 열거형
        // -------------------------------------------------------

        public enum PlacerState { Idle, Placing, Dropped }

        // -------------------------------------------------------
        // Inspector 노출 필드
        // -------------------------------------------------------

        [Header("References")]
        [SerializeField] private GridSystem _gridSystem;
        [SerializeField] private PathFinder _pathFinder;
        [SerializeField] private WallPreview _wallPreview;
        [SerializeField] private WallPlacementUI _wallPlacementUI;

        [Tooltip("씬에 배치될 WallObject 프리팹 (WallObject 컴포넌트 필수)")]
        [SerializeField] private WallObject _wallObjectPrefab;

        // -------------------------------------------------------
        // 내부 상태
        // -------------------------------------------------------

        private PlacerState _state = PlacerState.Idle;
        private WallData _currentData;
        private int _rotationSteps;

        /// <summary>현재 미리보기의 앵커 셀 (마우스 위치에 해당하는 그리드 셀)</summary>
        private Vector2Int _currentAnchorCell;

        /// <summary>현재 회전이 적용된 오프셋 배열</summary>
        private Vector2Int[] _currentOffsets;

        /// <summary>
        /// 현재 미리보기 위치가 유효한지 여부.
        /// - Placing 상태: 범위 + 겹침 검사 결과
        /// - Dropped 상태: 범위 + 겹침 + A* 경로 검사 결과
        /// </summary>
        private bool _isCurrentValid;

        // -------------------------------------------------------
        // 프로퍼티 (WallPlacementUI에서 읽음)
        // -------------------------------------------------------

        public PlacerState State => _state;
        public bool IsCurrentValid => _isCurrentValid;

        // -------------------------------------------------------
        // 유니티 생명주기
        // -------------------------------------------------------

        private void Update()
        {
            if (_state == PlacerState.Idle) return;

            if (_state == PlacerState.Placing)
            {
                TrackMouseAndShowPreview();
            }

            HandleRightClick();
            HandleLeftClick();
        }

        // -------------------------------------------------------
        // 외부 진입점
        // -------------------------------------------------------

        /// <summary>
        /// 특정 WallData로 배치를 시작한다.
        /// WallPlacementUI의 팔레트 버튼 또는 카드 시스템(4단계)이 호출.
        /// </summary>
        public void StartPlacing(WallData data)
        {
            _currentData = data;
            _rotationSteps = 0;
            _currentOffsets = null;
            _isCurrentValid = false;
            _state = PlacerState.Placing;
        }

        // -------------------------------------------------------
        // Placing 상태: 마우스 추적 + 겹침 검사
        // -------------------------------------------------------

        /// <summary>
        /// 마우스 위치를 그리드 좌표로 변환하고 미리보기를 갱신한다.
        /// A*는 실행하지 않고 범위/겹침 검사만 수행한다.
        /// </summary>
        private void TrackMouseAndShowPreview()
        {
            Vector2 mouseScreen = Mouse.current.position.ReadValue();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, 0f));
            worldPos.z = 0f;

            Vector2Int anchorCell = _gridSystem.WorldToGridPosition(worldPos);
            Vector2Int[] offsets = _currentData.GetRotatedOffsets(_rotationSteps);

            _currentAnchorCell = anchorCell;
            _currentOffsets = offsets;
            _isCurrentValid = CheckBoundsAndOverlap(offsets, anchorCell);

            _wallPreview.Show(offsets, anchorCell, _isCurrentValid, _gridSystem);
        }

        /// <summary>
        /// 오프셋 배열의 모든 셀이 그리드 범위 안에 있고 기존 벽과 겹치지 않는지 확인한다.
        /// </summary>
        private bool CheckBoundsAndOverlap(Vector2Int[] offsets, Vector2Int anchor)
        {
            foreach (Vector2Int offset in offsets)
            {
                Vector2Int cell = anchor + offset;
                if (!_gridSystem.IsInBounds(cell)) return false;
                if (_gridSystem.IsWall(cell)) return false;
            }
            return true;
        }

        // -------------------------------------------------------
        // 입력 처리
        // -------------------------------------------------------

        /// <summary>
        /// 우클릭: 벽을 90도 시계 방향으로 회전하고 Placing 상태로 복귀한다.
        /// </summary>
        private void HandleRightClick()
        {
            if (!Mouse.current.rightButton.wasPressedThisFrame) return;

            _rotationSteps = (_rotationSteps + 1) % 4;
            // Dropped 상태에서 회전하면 Placing으로 돌아가 재배치 가능
            _state = PlacerState.Placing;
            _currentOffsets = null;
        }

        /// <summary>
        /// 좌클릭 처리.
        /// UI 영역 클릭은 무시한다.
        /// Placing: 유효하면 드롭 → A* 실행 → Dropped
        /// Dropped: 다시 들어올려 Placing으로 복귀
        /// </summary>
        private void HandleLeftClick()
        {
            if (!Mouse.current.leftButton.wasPressedThisFrame) return;

            // UI 버튼 위에서는 월드 클릭 처리 건너뜀
            if (_wallPlacementUI != null && _wallPlacementUI.IsMouseOverUI) return;

            switch (_state)
            {
                case PlacerState.Placing:
                    TryDrop();
                    break;
                case PlacerState.Dropped:
                    // 다시 들어올려 위치 조정 가능
                    _state = PlacerState.Placing;
                    break;
            }
        }

        // -------------------------------------------------------
        // 드롭 처리 (A* 검증)
        // -------------------------------------------------------

        /// <summary>
        /// 현재 위치에 드롭을 시도한다.
        /// 겹침/범위 오류 시 드롭 불가.
        /// 유효하면 A*를 1회 실행해 경로 차단 여부를 확인하고 Dropped 상태로 전환.
        /// </summary>
        private void TryDrop()
        {
            // 겹침이나 범위 오류가 있으면 드롭 불가
            if (!_isCurrentValid) return;

            // A* 경로 차단 검증 (드롭 시점에 1회만 실행)
            List<Vector2Int> cells = GetCurrentCells();
            bool pathOk = ValidatePathAfterWall(cells);
            _isCurrentValid = pathOk;
            _state = PlacerState.Dropped;

            // 드롭 결과를 미리보기에 반영 (경로 차단이면 빨간색)
            _wallPreview.Show(_currentOffsets, _currentAnchorCell, pathOk, _gridSystem);
        }

        /// <summary>
        /// 현재 앵커와 오프셋으로 점유될 셀 목록을 반환한다.
        /// </summary>
        private List<Vector2Int> GetCurrentCells()
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            foreach (Vector2Int offset in _currentOffsets)
            {
                cells.Add(_currentAnchorCell + offset);
            }
            return cells;
        }

        /// <summary>
        /// 벽 셀들을 임시로 GridSystem에 등록한 후 A*를 실행해 경로가 남아있는지 확인한다.
        /// 검사 후 반드시 임시 벽을 해제한다.
        /// </summary>
        private bool ValidatePathAfterWall(List<Vector2Int> cells)
        {
            // 임시 벽 적용
            foreach (Vector2Int cell in cells) _gridSystem.SetWall(cell, true);

            bool hasPath = _pathFinder.HasPath(_gridSystem.SpawnPoint, _gridSystem.GoalPoint);

            // 임시 벽 해제
            foreach (Vector2Int cell in cells) _gridSystem.SetWall(cell, false);

            return hasPath;
        }

        // -------------------------------------------------------
        // 확정 / 취소
        // -------------------------------------------------------

        /// <summary>
        /// 현재 위치에 벽을 확정 배치한다.
        /// Dropped 상태이고 유효할 때만 동작한다.
        /// WallPlacementUI의 확정 버튼이 호출한다.
        /// </summary>
        public void Confirm()
        {
            if (_state != PlacerState.Dropped || !_isCurrentValid) return;

            List<Vector2Int> cells = GetCurrentCells();

            // WallObject 인스턴스 생성 및 배치 확정
            WallObject wallObj = Instantiate(
                _wallObjectPrefab,
                _gridSystem.GridToWorldPosition(_currentAnchorCell),
                Quaternion.identity
            );
            wallObj.Place(cells, _currentData, _gridSystem);

            ResetState();
        }

        /// <summary>
        /// 배치를 취소하고 Idle 상태로 돌아간다.
        /// WallPlacementUI의 취소 버튼이 호출한다.
        /// </summary>
        public void Cancel()
        {
            ResetState();
        }

        /// <summary>
        /// 상태를 초기화하고 Idle로 전환한다.
        /// </summary>
        private void ResetState()
        {
            _wallPreview.Hide();
            _state = PlacerState.Idle;
            _currentData = null;
            _currentOffsets = null;
            _isCurrentValid = false;
        }
    }
}
