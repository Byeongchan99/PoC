using System.Collections.Generic;
using UnityEngine;

namespace POC4
{
    /// <summary>
    /// 씬에 실제로 배치된 벽 오브젝트.
    /// 자신이 점유한 그리드 셀 목록을 관리하고,
    /// 배치 확정 시 GridSystem에 해당 셀들을 벽으로 등록한다.
    /// </summary>
    public class WallObject : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector 노출 필드
        // -------------------------------------------------------

        [Header("Visual")]
        [SerializeField] private Color _wallColor = new Color(0.4f, 0.7f, 1f, 0.85f);

        // -------------------------------------------------------
        // 내부 상태
        // -------------------------------------------------------

        private List<Vector2Int> _occupiedCells = new List<Vector2Int>();
        private GridSystem _gridSystem;
        private WallData _wallData;

        // -------------------------------------------------------
        // 프로퍼티
        // -------------------------------------------------------

        /// <summary>이 벽 위에 타워가 설치되었는지 여부 (3단계에서 사용)</summary>
        public bool HasTower { get; private set; }

        public WallData WallData => _wallData;
        public IReadOnlyList<Vector2Int> OccupiedCells => _occupiedCells;

        // -------------------------------------------------------
        // 배치 확정
        // -------------------------------------------------------

        /// <summary>
        /// 벽을 지정한 셀 목록에 배치하고 GridSystem에 등록한다.
        /// WallPlacer의 Confirm()에서 호출한다.
        /// </summary>
        public void Place(List<Vector2Int> cells, WallData data, GridSystem gridSystem)
        {
            _occupiedCells = cells;
            _wallData = data;
            _gridSystem = gridSystem;

            // GridSystem에 벽으로 등록
            foreach (Vector2Int cell in _occupiedCells)
            {
                _gridSystem.SetWall(cell, true);
            }

            // 각 셀 위치에 시각적 사각형 생성
            RenderCells();
        }

        /// <summary>
        /// 타워 설치 여부를 기록한다. 타워 설치 시스템(3단계)에서 호출.
        /// </summary>
        public void SetTowerPlaced(bool placed)
        {
            HasTower = placed;
        }

        // -------------------------------------------------------
        // 시각적 렌더링
        // -------------------------------------------------------

        /// <summary>
        /// 점유 셀 각각에 SpriteRenderer를 붙인 자식 오브젝트를 생성한다.
        /// </summary>
        private void RenderCells()
        {
            Sprite sprite = CreateWhiteSprite();
            float scale = _gridSystem.CellSize * 0.85f;

            foreach (Vector2Int cell in _occupiedCells)
            {
                GameObject cellObj = new GameObject($"WallCell_{cell.x}_{cell.y}");
                cellObj.transform.SetParent(transform);
                cellObj.transform.position = _gridSystem.GridToWorldPosition(cell);
                cellObj.transform.localScale = Vector3.one * scale;

                SpriteRenderer sr = cellObj.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = _wallColor;
                sr.sortingOrder = 2;
            }
        }

        /// <summary>
        /// 런타임에서 단색 흰색 스프라이트를 생성한다.
        /// 4×4 픽셀 텍스처를 사용해 필터링 아티팩트를 방지한다.
        /// </summary>
        private Sprite CreateWhiteSprite()
        {
            const int size = 4;
            Texture2D tex = new Texture2D(size, size) { filterMode = FilterMode.Point };
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            // pixelsPerUnit = size 이면 size픽셀 = 1유니티 단위, 스프라이트 크기 = 1×1
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        // -------------------------------------------------------
        // 정리
        // -------------------------------------------------------

        private void OnDestroy()
        {
            // 오브젝트가 제거될 때 GridSystem에서 벽 상태 해제
            // POC에서는 벽 제거 기능이 없지만, 씬 종료 등 예외 상황 대비
            if (_gridSystem == null) return;
            foreach (Vector2Int cell in _occupiedCells)
            {
                _gridSystem.SetWall(cell, false);
            }
        }
    }
}
