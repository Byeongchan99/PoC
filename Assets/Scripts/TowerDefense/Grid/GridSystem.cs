using UnityEngine;

namespace TowerDefense
{
    /// <summary>
    /// 그리드 전체를 관리하는 핵심 클래스.
    /// 그리드 크기, 셀 크기, 시작/목표 지점을 보관하고
    /// 그리드 좌표 <-> 월드 좌표 변환 기능을 제공한다.
    /// 씬 내 빈 GameObject에 단일 컴포넌트로 붙여서 사용.
    /// </summary>
    public class GridSystem : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector 노출 필드
        // -------------------------------------------------------

        [Header("Grid Size")]
        [SerializeField] private int _width = 10;
        [SerializeField] private int _height = 10;
        [SerializeField] private float _cellSize = 1f;

        [Header("Key Positions (Grid Coordinates)")]
        [Tooltip("적이 등장하는 시작 지점 (그리드 좌표)")]
        [SerializeField] private Vector2Int _spawnPoint = new Vector2Int(0, 5);

        [Tooltip("적이 도달해야 하는 목표 지점 (그리드 좌표)")]
        [SerializeField] private Vector2Int _goalPoint = new Vector2Int(9, 5);

        // -------------------------------------------------------
        // 내부 데이터
        // -------------------------------------------------------

        /// <summary>
        /// 각 셀의 상태를 저장하는 2차원 배열.
        /// true = 벽(통과 불가), false = 비어있음(통과 가능)
        /// </summary>
        private bool[,] _isWall;

        // -------------------------------------------------------
        // 프로퍼티 (읽기 전용)
        // -------------------------------------------------------

        public int Width => _width;
        public int Height => _height;
        public float CellSize => _cellSize;
        public Vector2Int SpawnPoint => _spawnPoint;
        public Vector2Int GoalPoint => _goalPoint;

        // -------------------------------------------------------
        // 유니티 생명주기
        // -------------------------------------------------------

        private void Awake()
        {
            InitializeGrid();
        }

        // -------------------------------------------------------
        // 초기화
        // -------------------------------------------------------

        /// <summary>
        /// 그리드 배열을 초기화한다. 모든 셀을 비어있는 상태로 설정.
        /// </summary>
        private void InitializeGrid()
        {
            _isWall = new bool[_width, _height];

            for (int x = 0; x < _width; x++)
            {
                for (int y = 0; y < _height; y++)
                {
                    _isWall[x, y] = false;
                }
            }
        }

        // -------------------------------------------------------
        // 좌표 변환 메서드
        // -------------------------------------------------------

        /// <summary>
        /// 그리드 좌표(정수)를 월드 좌표(실수)로 변환한다.
        /// 반환값은 셀의 중심 위치.
        /// </summary>
        public Vector3 GridToWorldPosition(int x, int y)
        {
            // 그리드의 좌측 하단을 기준으로 셀 중앙 위치 계산
            Vector3 origin = transform.position;
            return origin + new Vector3(x * _cellSize + _cellSize * 0.5f,
                                        y * _cellSize + _cellSize * 0.5f,
                                        0f);
        }

        /// <summary>
        /// Vector2Int 오버로드.
        /// </summary>
        public Vector3 GridToWorldPosition(Vector2Int gridPos)
        {
            return GridToWorldPosition(gridPos.x, gridPos.y);
        }

        /// <summary>
        /// 월드 좌표(실수)를 그리드 좌표(정수)로 변환한다.
        /// 그리드 범위 밖이면 clamp 처리.
        /// </summary>
        public Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            Vector3 localPos = worldPos - transform.position;
            int x = Mathf.FloorToInt(localPos.x / _cellSize);
            int y = Mathf.FloorToInt(localPos.y / _cellSize);

            x = Mathf.Clamp(x, 0, _width - 1);
            y = Mathf.Clamp(y, 0, _height - 1);

            return new Vector2Int(x, y);
        }

        // -------------------------------------------------------
        // 셀 상태 접근 메서드
        // -------------------------------------------------------

        /// <summary>
        /// 해당 그리드 좌표가 그리드 범위 안에 있는지 확인한다.
        /// </summary>
        public bool IsInBounds(int x, int y)
        {
            return x >= 0 && x < _width && y >= 0 && y < _height;
        }

        /// <summary>
        /// Vector2Int 오버로드.
        /// </summary>
        public bool IsInBounds(Vector2Int gridPos)
        {
            return IsInBounds(gridPos.x, gridPos.y);
        }

        /// <summary>
        /// 해당 셀이 벽인지 반환한다.
        /// 범위 밖은 벽으로 취급.
        /// </summary>
        public bool IsWall(int x, int y)
        {
            if (!IsInBounds(x, y)) return true;
            return _isWall[x, y];
        }

        /// <summary>
        /// Vector2Int 오버로드.
        /// </summary>
        public bool IsWall(Vector2Int gridPos)
        {
            return IsWall(gridPos.x, gridPos.y);
        }

        /// <summary>
        /// 해당 셀의 벽 상태를 설정한다.
        /// </summary>
        public void SetWall(int x, int y, bool isWall)
        {
            if (!IsInBounds(x, y)) return;
            _isWall[x, y] = isWall;
        }

        /// <summary>
        /// Vector2Int 오버로드.
        /// </summary>
        public void SetWall(Vector2Int gridPos, bool isWall)
        {
            SetWall(gridPos.x, gridPos.y, isWall);
        }

        // -------------------------------------------------------
        // 에디터 전용 유효성 검사 (개발 편의)
        // -------------------------------------------------------

#if UNITY_EDITOR
        /// <summary>
        /// Inspector 값이 변경될 때마다 그리드를 재초기화한다.
        /// 에디터에서 수치를 바꾸면 즉시 반영됨.
        /// </summary>
        private void OnValidate()
        {
            _width = Mathf.Max(1, _width);
            _height = Mathf.Max(1, _height);
            _cellSize = Mathf.Max(0.1f, _cellSize);

            // 시작/목표 지점이 그리드 범위 내에 있도록 보정
            _spawnPoint.x = Mathf.Clamp(_spawnPoint.x, 0, _width - 1);
            _spawnPoint.y = Mathf.Clamp(_spawnPoint.y, 0, _height - 1);
            _goalPoint.x = Mathf.Clamp(_goalPoint.x, 0, _width - 1);
            _goalPoint.y = Mathf.Clamp(_goalPoint.y, 0, _height - 1);
        }
#endif
    }
}
