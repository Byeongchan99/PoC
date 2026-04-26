using TMPro;
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
    ///   게임 시작 → 1라운드 준비 페이즈 → "전투 시작" 버튼 → 전투 페이즈
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
        // Inspector 노출 필드 - Canvas UI
        // -------------------------------------------------------

        [Header("Status UI")]
        [Tooltip("라운드 번호를 표시하는 TMP_Text")]
        [SerializeField] private TMP_Text _roundText;

        [Tooltip("플레이어 HP를 표시하는 TMP_Text")]
        [SerializeField] private TMP_Text _hpText;

        [Tooltip("현재 페이즈를 표시하는 TMP_Text")]
        [SerializeField] private TMP_Text _phaseText;

        [Header("Combat Start Button")]
        [Tooltip("준비 페이즈에만 표시할 '전투 시작' 버튼 GameObject")]
        [SerializeField] private GameObject _startCombatButtonObject;

        [Header("Game Over UI")]
        [Tooltip("게임 오버 시 표시할 패널 GameObject")]
        [SerializeField] private GameObject _gameOverPanel;

        [Tooltip("게임 오버 패널에 최종 라운드를 표시하는 TMP_Text")]
        [SerializeField] private TMP_Text _gameOverRoundText;

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
            _gameOverPanel?.SetActive(false);
        }

        private void Start()
        {
            ValidateReferences();
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
            _startCombatButtonObject?.SetActive(true);

            UpdateStatusUI();

            Debug.Log($"[GameManager] ===== {_currentRound}라운드 준비 페이즈 시작 =====");
        }

        /// <summary>
        /// 전투 페이즈로 전환한다.
        /// 진행 중인 배치를 취소하고, 플레이어 조작을 비활성화한 뒤 적 스폰을 시작한다.
        /// Canvas 버튼 OnClick에 연결하거나 직접 호출한다.
        /// </summary>
        public void StartCombat()
        {
            if (_currentPhase != GamePhase.Preparation || _isGameOver) return;

            // 진행 중인 배치를 취소해 미완성 상태가 남지 않도록 한다.
            _wallPlacer?.Cancel();
            _towerPlacer?.CancelPlacing();

            _currentPhase = GamePhase.Combat;
            SetPlayerControlEnabled(false);
            _startCombatButtonObject?.SetActive(false);

            int enemyCount = _initialEnemyCount + (_currentRound - 1) * _enemyCountIncreasePerRound;
            // 스케일 인덱스는 0부터 시작한다 (1라운드=0, 2라운드=1, ...).
            int scaleIndex = _currentRound - 1;

            UpdateStatusUI();

            Debug.Log($"[GameManager] ===== {_currentRound}라운드 전투 페이즈 시작. 적 {enemyCount}마리 =====");

            _enemySpawner?.StartSpawning(enemyCount, scaleIndex);
        }

        // -------------------------------------------------------
        // 플레이어 조작 활성화 / 비활성화
        // -------------------------------------------------------

        /// <summary>
        /// 준비 페이즈 전용 컴포넌트들의 enabled를 일괄 설정한다.
        /// enabled = false이면 Update 등 유니티 이벤트가 호출되지 않는다.
        /// </summary>
        private void SetPlayerControlEnabled(bool isEnabled)
        {
            if (_wallPlacer != null)      _wallPlacer.enabled      = isEnabled;
            if (_wallPlacementUI != null) _wallPlacementUI.enabled = isEnabled;
            if (_towerPlacer != null)     _towerPlacer.enabled     = isEnabled;
            if (_handUI != null)          _handUI.enabled          = isEnabled;
            if (_cardCraftingUI != null)  _cardCraftingUI.enabled  = isEnabled;
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

            UpdateStatusUI();

            Debug.Log($"[GameManager] 플레이어 피해! HP: {_currentPlayerHp}/{_maxPlayerHp}");

            if (_currentPlayerHp <= 0)
                TriggerGameOver();
        }

        // -------------------------------------------------------
        // 게임 오버
        // -------------------------------------------------------

        /// <summary>
        /// 게임 오버 상태로 전환한다.
        /// 스폰을 중단하고 플레이어 조작을 비활성화한 뒤 게임 오버 패널을 표시한다.
        /// </summary>
        private void TriggerGameOver()
        {
            _isGameOver = true;
            _enemySpawner?.StopSpawning();
            SetPlayerControlEnabled(false);
            _startCombatButtonObject?.SetActive(false);

            if (_gameOverPanel != null)
            {
                _gameOverPanel.SetActive(true);
                if (_gameOverRoundText != null)
                    _gameOverRoundText.text = $"최종 라운드: {_currentRound}";
            }

            Debug.Log("[GameManager] ===== 게임 오버 =====");
        }

        // -------------------------------------------------------
        // 게임 재시작
        // -------------------------------------------------------

        /// <summary>
        /// 모든 상태를 초기화하고 1라운드 준비 페이즈부터 다시 시작한다.
        /// 게임 오버 패널의 '다시 시작' 버튼 OnClick에 연결한다.
        /// </summary>
        public void RestartGame()
        {
            // 씬에 남아 있는 적, 타워, 벽 제거
            Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            foreach (Enemy e in enemies) Destroy(e.gameObject);

            Tower[] towers = FindObjectsByType<Tower>(FindObjectsSortMode.None);
            foreach (Tower t in towers) Destroy(t.gameObject);

            WallObject[] walls = FindObjectsByType<WallObject>(FindObjectsSortMode.None);
            foreach (WallObject w in walls) Destroy(w.gameObject);

            // 상태 초기화
            _currentRound = 0;
            _currentPlayerHp = _maxPlayerHp;
            _isGameOver = false;

            _gameOverPanel?.SetActive(false);
            _enemySpawner?.StopSpawning();

            EnterPreparationPhase();

            Debug.Log("[GameManager] 게임 재시작.");
        }

        // -------------------------------------------------------
        // Canvas UI 갱신
        // -------------------------------------------------------

        /// <summary>
        /// 라운드, HP, 페이즈 텍스트를 현재 상태로 갱신한다.
        /// 페이즈 전환 및 HP 변화 시 호출한다.
        /// </summary>
        private void UpdateStatusUI()
        {
            if (_roundText != null)
                _roundText.text = $"라운드: {_currentRound}";

            if (_hpText != null)
                _hpText.text = $"HP: {_currentPlayerHp} / {_maxPlayerHp}";

            if (_phaseText != null)
                _phaseText.text = _currentPhase == GamePhase.Preparation ? "준비 페이즈" : "전투 페이즈";
        }

        // -------------------------------------------------------
        // Inspector ContextMenu (디버그)
        // -------------------------------------------------------

        [ContextMenu("Debug: 전투 시작")]
        private void DebugStartCombat()
        {
            StartCombat();
        }

        [ContextMenu("Debug: 다음 라운드")]
        private void DebugNextRound()
        {
            _enemySpawner?.StopSpawning();
            Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            foreach (Enemy e in enemies) Destroy(e.gameObject);
            EnterPreparationPhase();
        }

        [ContextMenu("Debug: 게임 재시작")]
        private void DebugRestartGame()
        {
            RestartGame();
        }
    }
}
