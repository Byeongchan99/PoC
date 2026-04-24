using UnityEngine;

namespace POC4
{
    /// <summary>
    /// 그리드를 시각적으로 표시하는 클래스.
    /// Scene 뷰에서는 Gizmo로 그리드를 그리고,
    /// 런타임에서는 LineRenderer로 그리드 라인을 표시한다.
    /// GridSystem과 같은 GameObject에 붙이거나 참조를 연결해서 사용.
    /// </summary>
    [RequireComponent(typeof(GridSystem))]
    public class GridVisualizer : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector 노출 필드
        // -------------------------------------------------------

        [Header("Gizmo Colors")]
        [SerializeField] private Color _gridLineColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        [SerializeField] private Color _spawnPointColor = Color.green;
        [SerializeField] private Color _goalPointColor = Color.red;
        [SerializeField] private Color _wallCellColor = new Color(1f, 0.3f, 0.3f, 0.4f);

        [Header("Runtime Grid Lines")]
        [Tooltip("런타임에서 그리드 라인을 화면에 표시할지 여부")]
        [SerializeField] private bool _showRuntimeGrid = true;
        [SerializeField] private Color _runtimeLineColor = new Color(0.4f, 0.4f, 0.4f, 0.3f);

        // -------------------------------------------------------
        // 내부 참조
        // -------------------------------------------------------

        private GridSystem _gridSystem;

        // 런타임 그리드 라인을 그리는 LineRenderer 배열
        // 가로선 height+1 개 + 세로선 width+1 개
        private LineRenderer[] _lineRenderers;

        // -------------------------------------------------------
        // 유니티 생명주기
        // -------------------------------------------------------

        private void Awake()
        {
            _gridSystem = GetComponent<GridSystem>();
        }

        private void Start()
        {
            if (_showRuntimeGrid)
            {
                CreateRuntimeGridLines();
            }
        }

        // -------------------------------------------------------
        // 런타임 그리드 라인 생성
        // -------------------------------------------------------

        /// <summary>
        /// LineRenderer를 사용해 런타임 그리드 라인을 생성한다.
        /// 가로선(height+1)과 세로선(width+1)을 모두 생성.
        /// </summary>
        private void CreateRuntimeGridLines()
        {
            int width = _gridSystem.Width;
            int height = _gridSystem.Height;
            float cellSize = _gridSystem.CellSize;
            Vector3 origin = transform.position;

            int totalLines = (width + 1) + (height + 1);
            _lineRenderers = new LineRenderer[totalLines];

            int index = 0;

            // 가로선 (y축 방향)
            for (int y = 0; y <= height; y++)
            {
                _lineRenderers[index] = CreateLineRenderer();
                Vector3 start = origin + new Vector3(0f, y * cellSize, 0f);
                Vector3 end = origin + new Vector3(width * cellSize, y * cellSize, 0f);
                _lineRenderers[index].SetPosition(0, start);
                _lineRenderers[index].SetPosition(1, end);
                index++;
            }

            // 세로선 (x축 방향)
            for (int x = 0; x <= width; x++)
            {
                _lineRenderers[index] = CreateLineRenderer();
                Vector3 start = origin + new Vector3(x * cellSize, 0f, 0f);
                Vector3 end = origin + new Vector3(x * cellSize, height * cellSize, 0f);
                _lineRenderers[index].SetPosition(0, start);
                _lineRenderers[index].SetPosition(1, end);
                index++;
            }
        }

        /// <summary>
        /// 그리드 라인 하나를 위한 LineRenderer 컴포넌트를 생성하고 기본 설정한다.
        /// </summary>
        private LineRenderer CreateLineRenderer()
        {
            GameObject lineObj = new GameObject("GridLine");
            lineObj.transform.SetParent(transform);

            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = 0.02f;
            lr.endWidth = 0.02f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = _runtimeLineColor;
            lr.endColor = _runtimeLineColor;
            lr.useWorldSpace = true;

            return lr;
        }

        // -------------------------------------------------------
        // Scene 뷰 Gizmo
        // -------------------------------------------------------

        /// <summary>
        /// Scene 뷰에서 그리드 구조, 시작/목표 지점, 벽 셀을 Gizmo로 표시한다.
        /// 에디터에서만 동작하며 런타임 성능에 영향 없음.
        /// </summary>
        private void OnDrawGizmos()
        {
            // GridSystem이 없으면 직접 참조 시도 (에디터 전용)
            if (_gridSystem == null)
            {
                _gridSystem = GetComponent<GridSystem>();
            }

            if (_gridSystem == null) return;

            DrawGridLines();
            DrawSpecialCells();
        }

        /// <summary>
        /// 그리드 전체 라인을 Gizmo로 그린다.
        /// </summary>
        private void DrawGridLines()
        {
            int width = _gridSystem.Width;
            int height = _gridSystem.Height;
            float cellSize = _gridSystem.CellSize;
            Vector3 origin = transform.position;

            Gizmos.color = _gridLineColor;

            // 가로선
            for (int y = 0; y <= height; y++)
            {
                Vector3 start = origin + new Vector3(0f, y * cellSize, 0f);
                Vector3 end = origin + new Vector3(width * cellSize, y * cellSize, 0f);
                Gizmos.DrawLine(start, end);
            }

            // 세로선
            for (int x = 0; x <= width; x++)
            {
                Vector3 start = origin + new Vector3(x * cellSize, 0f, 0f);
                Vector3 end = origin + new Vector3(x * cellSize, height * cellSize, 0f);
                Gizmos.DrawLine(start, end);
            }
        }

        /// <summary>
        /// 시작 지점, 목표 지점, 벽 셀을 색상 큐브로 표시한다.
        /// </summary>
        private void DrawSpecialCells()
        {
            float cellSize = _gridSystem.CellSize;
            float cubeSize = cellSize * 0.9f;

            // 시작 지점 (초록색)
            Gizmos.color = _spawnPointColor;
            Vector3 spawnWorld = _gridSystem.GridToWorldPosition(_gridSystem.SpawnPoint);
            Gizmos.DrawCube(spawnWorld, new Vector3(cubeSize, cubeSize, 0.01f));

            // 목표 지점 (빨간색)
            Gizmos.color = _goalPointColor;
            Vector3 goalWorld = _gridSystem.GridToWorldPosition(_gridSystem.GoalPoint);
            Gizmos.DrawCube(goalWorld, new Vector3(cubeSize, cubeSize, 0.01f));

            // 벽 셀 (반투명 빨간색)
            Gizmos.color = _wallCellColor;
            for (int x = 0; x < _gridSystem.Width; x++)
            {
                for (int y = 0; y < _gridSystem.Height; y++)
                {
                    if (_gridSystem.IsWall(x, y))
                    {
                        Vector3 cellWorld = _gridSystem.GridToWorldPosition(x, y);
                        Gizmos.DrawCube(cellWorld, new Vector3(cubeSize, cubeSize, 0.01f));
                    }
                }
            }
        }
    }
}
