using System.Collections;
using UnityEngine;

namespace POC4
{
    /// <summary>
    /// 게임 전체 흐름을 관리하는 클래스.
    ///
    /// 준비(Preparation) ↔ 전투(Combat) 두 페이즈를 전환하며,
    /// 플레이어 HP 추적, 라운드 스케일링, 승리/패배 조건을 처리한다.
    ///
    /// 흐름:
    ///   게임 시작 → 1라운드 준비 페이즈 → "전투 시작" 클릭 → 전투 페이즈
    ///   → 모든 적 처치 → 다음 라운드 준비 페이즈 → ...
    ///   → 플레이어 HP 0 → 게임 오버
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // -------------------------------------------------------
        // 페이즈 열거형
        // -------------------------------------------------------

        public enum GamePhase { Preparation, Combat }

        // -------------------------------------------------------
        // Inspector 노출 필드 - 참조
        // -------------------------------------------------------

        [Header("References")]
        [Tooltip("코스트(재화) 관리 컴포넌트")]
        [SerializeField] private CostManager _costManager;

        [Tooltip("적 스폰 컴포넌트")]
        [SerializeField] private EnemySpawner _enemySpawner;

        [Tooltip("벽 배치 컴포넌트 (준비 페이즈에만 활성화)")]
        [SerializeField] private WallPlacer _wallPlacer;

        [Tooltip("벽 선택 UI 컴포넌트 (준비 페이즈에만 활성화)")]
        [SerializeField] private WallPlacementUI _wallPlacementUI;

        [Tooltip("타워 배치 컴포넌트 (준비 페이즈에만 활성화)")]
        [SerializeField] private TowerPlacer _towerPlacer;

        [Tooltip("핸드 UI 컴포넌트 (준비 페이즈에만 활성화)")]
        [SerializeField] private HandUI _handUI;

        [Tooltip("카드 제작 UI 컴포넌트 (준비 페이즈에만 활성화)")]
        [SerializeField] private CardCraftingUI _cardCraftingUI;

        // -------------------------------------------------------
        // Inspector 노출 필드 - 게임 설정
        // -------------------------------------------------------

        [Header("Player HP")]
        [Tooltip("플레이어 최대 HP")]
        [SerializeField] private int _maxPlayerHp = 10;

        [Header("Round Scaling")]
        [Tooltip("1라운드에 스폰되는 적 수")]
        [SerializeField] private int _initialEnemyCount = 5;

        [Tooltip("라운드마다 증가하는 적 수")]
        [SerializeField] private int _enemyCountIncreasePerRound = 2;

        // -------------------------------------------------------
        // 내부 상태
        // -------------------------------------------------------

        private GamePhase _currentPhase;
        private int _currentRound;
        private int _currentPlayerHp;
        private bool _isGameOver;

        // -------------------------------------------------------
        // 프로퍼티
        // -------------------------------------------------------

        public GamePhase CurrentPhase => _currentPhase;
        public int CurrentRound => _currentRound;
        public int CurrentPlayerHp => _currentPlayerHp;
        public bool IsGameOver => _isGameOver;

        // -------------------------------------------------------
        // 유니티 생명주기
        // -------------------------------------------------------

        private void Awake()
        {
            _currentPlayerHp = _maxPlayerHp;
        }

        private void Start()
        {
            ValidateReferences();
            // 게임 시작 시 1라운드 준비 페이즈로 진입한다.
            EnterPreparationPhase();
        }

        private void OnEnable()
        {
            if (_enemySpawner != null)
                _enemySpawner.OnAllEnemiesDefeated += HandleAllEnemiesDefeated;

            Enemy.OnAnyEnemyReachedGoal += HandleEnemyReachedGoal;
        }

        private void OnDisable()
        {
            if (_enemySpawner != null)
                _enemySpawner.OnAllEnemiesDefeated -= HandleAllEnemiesDefeated;

            Enemy.OnAnyEnemyReachedGoal -= HandleEnemyReachedGoal;
        }

        // -------------------------------------------------------
        // 유효성 검사
        // -------------------------------------------------------

        private void ValidateReferences()
        {
            if (_costManager == null)
                Debug.LogError("[GameManager] CostManager가 Inspector에 연결되지 않았습니다.");
            if (_enemySpawner == null)
                Debug.LogError("[GameManager] EnemySpawner가 Inspector에 연결되지 않았습니다.");
            if (_wallPlacer == null)
                Debug.LogError("[GameManager] WallPlacer가 Inspector에 연결되지 않았습니다.");
            if (_wallPlacementUI == null)
                Debug.LogError("[GameManager] WallPlacementUI가 Inspector에 연결되지 않았습니다.");
            if (_towerPlacer == null)
                Debug.LogError("[GameManager] TowerPlacer가 Inspector에 연결되지 않았습니다.");
            if (_handUI == null)
                Debug.LogError("[GameManager] HandUI가 Inspector에 연결되지 않았습니다.");
            if (_cardCraftingUI == null)
                Debug.LogError("[GameManager] CardCraftingUI가 Inspector에 연결되지 않았습니다.");
        }

        // -------------------------------------------------------
        // 페이즈 전환
        // -------------------------------------------------------

        /// <summary>
        /// 준비 페이즈로 전환한다.
        /// 라운드를 1 증가시키고, 코스트를 지급하며, 플레이어 조작을 활성화한다.
        /// </summary>
        private void EnterPreparationPhase()
        {
            _currentPhase = GamePhase.Preparation;
            _currentRound++;

            _costManager?.AddRoundCost();
            SetPlayerControlEnabled(true);

            Debug.Log($"[GameManager] ===== {_currentRound}라운드 준비 페이즈 시작 =====");
        }

        /// <summary>
        /// 전투 페이즈로 전환한다.
        /// 진행 중인 배치를 취소하고, 플레이어 조작을 비활성화한 뒤 적 스폰을 시작한다.
        /// </summary>
        private void EnterCombatPhase()
        {
            // 진행 중인 배치를 먼저 취소해 미완성 상태가 남지 않도록 한다.
            _wallPlacer?.Cancel();
            _towerPlacer?.CancelPlacing();

            _currentPhase = GamePhase.Combat;
            SetPlayerControlEnabled(false);

            int enemyCount = _initialEnemyCount + (_currentRound - 1) * _enemyCountIncreasePerRound;
            // 스케일 인덱스는 0부터 시작한다 (1라운드=0, 2라운드=1, ...).
            int scaleIndex = _currentRound - 1;

            Debug.Log($"[GameManager] ===== {_currentRound}라운드 전투 페이즈 시작. 적 {enemyCount}마리 =====");

            _enemySpawner?.StartSpawning(enemyCount, scaleIndex);
        }

        // -------------------------------------------------------
        // 플레이어 조작 활성화 / 비활성화
        // -------------------------------------------------------

        /// <summary>
        /// 준비 페이즈 전용 컴포넌트들의 enabled를 일괄 설정한다.
        /// enabled = false 이면 Update, OnGUI 등 유니티 이벤트가 호출되지 않는다.
        /// </summary>
        private void SetPlayerControlEnabled(bool isEnabled)
        {
            if (_wallPlacer != null)       _wallPlacer.enabled       = isEnabled;
            if (_wallPlacementUI != null)  _wallPlacementUI.enabled  = isEnabled;
            if (_towerPlacer != null)      _towerPlacer.enabled      = isEnabled;
            if (_handUI != null)           _handUI.enabled           = isEnabled;
            if (_cardCraftingUI != null)   _cardCraftingUI.enabled   = isEnabled;
        }

        // -------------------------------------------------------
        // 이벤트 핸들러
        // -------------------------------------------------------

        /// <summary>
        /// EnemySpawner.OnAllEnemiesDefeated 이벤트를 받아 준비 페이즈로 전환한다.
        /// </summary>
        private void HandleAllEnemiesDefeated()
        {
            if (_isGameOver) return;

            Debug.Log("[GameManager] 모든 적 처치! 다음 라운드 준비 페이즈로 전환합니다.");
            EnterPreparationPhase();
        }

        /// <summary>
        /// Enemy.OnAnyEnemyReachedGoal 이벤트를 받아 플레이어 HP를 감소시킨다.
        /// HP가 0 이하면 게임 오버 처리한다.
        /// </summary>
        private void HandleEnemyReachedGoal(float damage)
        {
            if (_isGameOver) return;

            _currentPlayerHp -= Mathf.CeilToInt(damage);
            _currentPlayerHp = Mathf.Max(0, _currentPlayerHp);

            Debug.Log($"[GameManager] 플레이어 피해! HP: {_currentPlayerHp}/{_maxPlayerHp}");

            if (_currentPlayerHp <= 0)
            {
                TriggerGameOver();
            }
        }

        // -------------------------------------------------------
        // 게임 오버
        // -------------------------------------------------------

        /// <summary>
        /// 게임 오버 상태로 전환한다.
        /// 스폰을 중단하고 플레이어 조작을 비활성화한다.
        /// </summary>
        private void TriggerGameOver()
        {
            _isGameOver = true;
            _enemySpawner?.StopSpawning();
            SetPlayerControlEnabled(false);

            Debug.Log("[GameManager] ===== 게임 오버 =====");
        }

        // -------------------------------------------------------
        // OnGUI
        // -------------------------------------------------------

        private void OnGUI()
        {
            DrawStatusPanel();
            DrawCombatStartButton();
            DrawGameOverOverlay();
        }

        /// <summary>
        /// 화면 상단 중앙에 라운드·HP·페이즈 정보를 표시한다.
        /// </summary>
        private void DrawStatusPanel()
        {
            float panelWidth = 200f;
            float panelHeight = 75f;
            Rect rect = new Rect(Screen.width * 0.5f - panelWidth * 0.5f, 10f, panelWidth, panelHeight);

            GUILayout.BeginArea(rect);
            GUILayout.Label($"라운드: {_currentRound}");
            GUILayout.Label($"HP: {_currentPlayerHp} / {_maxPlayerHp}");
            string phaseLabel = _currentPhase == GamePhase.Preparation ? "준비 페이즈" : "전투 페이즈";
            GUILayout.Label($"페이즈: {phaseLabel}");
            GUILayout.EndArea();
        }

        /// <summary>
        /// 준비 페이즈에만 오른쪽에 "전투 시작" 버튼을 표시한다.
        /// CardCraftingUI(y=220 ~ 245+버튼) 아래에 배치한다.
        /// </summary>
        private void DrawCombatStartButton()
        {
            if (_isGameOver) return;
            if (_currentPhase != GamePhase.Preparation) return;

            Rect rect = new Rect(Screen.width - 200, 295, 190, 48);
            GUILayout.BeginArea(rect);
            if (GUILayout.Button("전투 시작", GUILayout.Height(44)))
            {
                EnterCombatPhase();
            }
            GUILayout.EndArea();
        }

        /// <summary>
        /// 게임 오버 시 화면 중앙에 반투명 오버레이를 표시한다.
        /// </summary>
        private void DrawGameOverOverlay()
        {
            if (!_isGameOver) return;

            // 반투명 검정 배경
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float w = 300f;
            float h = 120f;
            Rect rect = new Rect(Screen.width * 0.5f - w * 0.5f, Screen.height * 0.5f - h * 0.5f, w, h);

            GUILayout.BeginArea(rect);
            GUILayout.Label("게임 오버", GUI.skin.box);
            GUILayout.Label($"최종 라운드: {_currentRound}");
            GUILayout.Space(8f);
            if (GUILayout.Button("다시 시작"))
            {
                RestartGame();
            }
            GUILayout.EndArea();
        }

        // -------------------------------------------------------
        // 게임 재시작
        // -------------------------------------------------------

        /// <summary>
        /// 모든 상태를 초기화하고 1라운드 준비 페이즈부터 다시 시작한다.
        /// 씬에 남아 있는 적과 타워를 모두 제거한다.
        /// </summary>
        private void RestartGame()
        {
            // 씬에 남아 있는 적 제거
            Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            foreach (Enemy e in enemies) Destroy(e.gameObject);

            // 씬에 남아 있는 타워 제거
            Tower[] towers = FindObjectsByType<Tower>(FindObjectsSortMode.None);
            foreach (Tower t in towers) Destroy(t.gameObject);

            // 씬에 남아 있는 벽 제거
            WallObject[] walls = FindObjectsByType<WallObject>(FindObjectsSortMode.None);
            foreach (WallObject w in walls) Destroy(w.gameObject);

            // 상태 초기화
            _currentRound = 0;
            _currentPlayerHp = _maxPlayerHp;
            _isGameOver = false;

            _enemySpawner?.StopSpawning();

            EnterPreparationPhase();

            Debug.Log("[GameManager] 게임 재시작.");
        }

        // -------------------------------------------------------
        // Inspector ContextMenu (디버그)
        // -------------------------------------------------------

        /// <summary>
        /// 현재 준비 페이즈에서 즉시 전투 페이즈로 전환한다.
        /// </summary>
        [ContextMenu("Debug: 전투 시작")]
        private void DebugStartCombat()
        {
            if (_currentPhase == GamePhase.Preparation)
                EnterCombatPhase();
        }

        /// <summary>
        /// 현재 전투 페이즈를 강제 종료하고 다음 라운드 준비 페이즈로 전환한다.
        /// </summary>
        [ContextMenu("Debug: 다음 라운드")]
        private void DebugNextRound()
        {
            _enemySpawner?.StopSpawning();

            Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            foreach (Enemy e in enemies) Destroy(e.gameObject);

            EnterPreparationPhase();
        }

        /// <summary>
        /// 게임을 처음부터 다시 시작한다.
        /// </summary>
        [ContextMenu("Debug: 게임 재시작")]
        private void DebugRestartGame()
        {
            RestartGame();
        }
    }
}
