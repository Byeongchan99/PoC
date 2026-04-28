using System.Collections.Generic;
using UnityEngine;

namespace POC4
{
    /// <summary>
    /// 게임 시작 시 그리드에 기본 벽을 배치하는 클래스.
    /// Inspector에서 셀 좌표(Vector2Int) 목록을 직접 입력해 초기 벽을 설정한다.
    ///
    /// 각 셀은 독립적인 WallObject로 생성되므로 셀마다 타워를 설치할 수 있다.
    /// Scene 뷰 Gizmo로 배치될 셀을 미리 확인할 수 있다.
    /// </summary>
    public class DefaultWallSetup : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector 노출 필드
        // -------------------------------------------------------

        [Header("References")]
        [SerializeField] private GridSystem _gridSystem;

        [Tooltip("WallObject 컴포넌트가 포함된 프리팹")]
        [SerializeField] private WallObject _wallObjectPrefab;

        [Tooltip("기본 벽 전체에 적용할 WallData")]
        [SerializeField] private WallData _defaultWallData;

        [Header("Default Wall Cells")]
        [Tooltip("초기에 벽으로 설정할 셀 좌표 목록 (그리드 기준)")]
        [SerializeField] private List<Vector2Int> _wallCells = new List<Vector2Int>();

        // -------------------------------------------------------
        // 유니티 생명주기
        // -------------------------------------------------------

        private void Start()
        {
            if (_gridSystem == null || _wallObjectPrefab == null || _defaultWallData == null)
            {
                Debug.LogError("[DefaultWallSetup] 필수 참조가 연결되지 않았습니다.");
                return;
            }

            foreach (Vector2Int cell in _wallCells)
            {
                PlaceWallAt(cell);
            }
        }

        // -------------------------------------------------------
        // 벽 배치
        // -------------------------------------------------------

        /// <summary>
        /// 지정한 셀에 WallObject를 생성하고 그리드에 등록한다.
        /// 범위 밖이거나 이미 벽인 셀은 건너뛴다.
        /// </summary>
        private void PlaceWallAt(Vector2Int cell)
        {
            if (!_gridSystem.IsInBounds(cell))
            {
                Debug.LogWarning($"[DefaultWallSetup] 셀 {cell}은 그리드 범위 밖입니다. 건너뜁니다.");
                return;
            }

            if (_gridSystem.IsWall(cell))
            {
                Debug.LogWarning($"[DefaultWallSetup] 셀 {cell}에 이미 벽이 있습니다. 건너뜁니다.");
                return;
            }

            Vector3 worldPos = _gridSystem.GridToWorldPosition(cell);
            WallObject wallObj = Instantiate(_wallObjectPrefab, worldPos, Quaternion.identity);
            wallObj.Place(new List<Vector2Int> { cell }, _defaultWallData, _gridSystem);
        }

        // -------------------------------------------------------
        // Scene 뷰 Gizmo
        // -------------------------------------------------------

        /// <summary>
        /// Scene 뷰에서 배치될 셀을 파란색 사각형으로 미리 표시한다.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (_gridSystem == null || _wallCells == null) return;

            Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.5f);

            foreach (Vector2Int cell in _wallCells)
            {
                Vector3 center = _gridSystem.GridToWorldPosition(cell);
                float size = _gridSystem.CellSize * 0.9f;
                Gizmos.DrawCube(center, new Vector3(size, size, 0.01f));
            }
        }
    }
}
