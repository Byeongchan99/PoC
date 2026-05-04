using System;
using System.Collections.Generic;
using UnityEngine;

namespace POC6
{
    /// <summary>
    /// 게임 전체 흐름을 제어하는 상태 머신입니다.
    /// Init -> BuildPhase -> CombatPhase -> WaveResult -> CardSelection -> BuildPhase (반복)
    /// 웨이브 실패 시: WaveResult -> (스냅샷 복원) -> BuildPhase
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("설정")]
        [SerializeField] private GameConfig _config;

        [Header("카드 풀 (웨이브 클리어 후 제시될 전체 카드 목록)")]
        [SerializeField] private List<CardData> _cardPool = new();

        [Header("참조")]
        [SerializeField] private ShipGrid _shipGrid;
        [SerializeField] private PowerGraph _powerGraph;
        [SerializeField] private HealthSystem _healthSystem;
        [SerializeField] private ShipController _shipController;
        [SerializeField] private EnemySpawner _enemySpawner;
        [SerializeField] private WaveManager _waveManager;
        [SerializeField] private DefaultShipSetup _defaultShipSetup;
        [SerializeField] private DeckManager _deckManager;
        [SerializeField] private GoldSystem _goldSystem;

        // ────────────────────────────────────────────────
        // 이벤트
        // ────────────────────────────────────────────────

        /// <summary>게임 상태가 변경될 때 발행됩니다.</summary>
        public static event Action<GameState> OnGameStateChanged;

        /// <summary>웨이브 클리어 처리 완료 후 발행됩니다. (클리어한 웨이브 번호)</summary>
        public static event Action<int> OnWaveCleared;

        /// <summary>웨이브 실패 처리 완료 후 발행됩니다.</summary>
        public static event Action OnWaveFailed;

        /// <summary>모든 웨이브를 클리어했을 때 발행됩니다.</summary>
        public static event Action OnGameCleared;

        // ────────────────────────────────────────────────
        // 상태
        // ────────────────────────────────────────────────

        private GameState _currentState = GameState.Init;
        private WaveSnapshot _lastWaveSnapshot;
        private int _lastClearedWave = 0;

        public GameState CurrentState => _currentState;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            SubscribeEvents();
            ChangeState(GameState.Init);
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        // ────────────────────────────────────────────────
        // 이벤트 구독
        // ────────────────────────────────────────────────

        private void SubscribeEvents()
        {
            HealthSystem.OnDied += HandleShipDied;
            WaveManager.OnWaveCleared += HandleWaveCleared;
        }

        private void UnsubscribeEvents()
        {
            HealthSystem.OnDied -= HandleShipDied;
            WaveManager.OnWaveCleared -= HandleWaveCleared;
        }

        // ────────────────────────────────────────────────
        // 상태 전환
        // ────────────────────────────────────────────────

        /// <summary>
        /// 새로운 게임 상태로 전환합니다. 진입/퇴장 로직을 각 상태별로 처리합니다.
        /// </summary>
        private void ChangeState(GameState newState)
        {
            _currentState = newState;
            OnGameStateChanged?.Invoke(newState);

            switch (newState)
            {
                case GameState.Init:
                    HandleInit();
                    break;
                case GameState.BuildPhase:
                    HandleEnterBuildPhase();
                    break;
                case GameState.CombatPhase:
                    HandleEnterCombatPhase();
                    break;
                case GameState.WaveResult:
                    // WaveResult 진입은 HandleWaveCleared/HandleShipDied에서 처리
                    break;
                case GameState.CardSelection:
                    HandleEnterCardSelection();
                    break;
            }
        }

        // ────────────────────────────────────────────────
        // Init
        // ────────────────────────────────────────────────

        /// <summary>
        /// 게임을 초기화합니다. 기본 우주선 배치, 시작 덱 설정, 시작 골드 지급.
        /// GoldSystem, DeckManager는 연결되지 않아도 동작합니다.
        /// </summary>
        private void HandleInit()
        {
            _waveManager.Initialize();
            _goldSystem?.Initialize(_config != null ? _config.StartingGold : 0);
            _deckManager?.Initialize();

            // 기본 우주선 배치
            _defaultShipSetup.SetupDefaultShip();

            // 체력 초기화
            _healthSystem.Initialize();

            ChangeState(GameState.BuildPhase);
        }

        // ────────────────────────────────────────────────
        // Build Phase
        // ────────────────────────────────────────────────

        /// <summary>
        /// Build Phase: 시간 정지, 전투 비활성화, 배치 UI 활성화.
        /// </summary>
        private void HandleEnterBuildPhase()
        {
            // config가 없으면 기본값 0 (완전 정지)
            Time.timeScale = _config != null ? _config.BuildPhaseTimeScale : 0f;
            _shipController?.DisableControl();
        }

        /// <summary>
        /// 플레이어가 "다음 웨이브 시작" 버튼을 누를 때 UI에서 호출합니다.
        /// </summary>
        public void OnStartWaveButtonPressed()
        {
            if (_currentState != GameState.BuildPhase) return;

            // 웨이브 시작 전 스냅샷 저장 (실패 시 복원용)
            SaveWaveSnapshot();

            ChangeState(GameState.CombatPhase);
        }

        // ────────────────────────────────────────────────
        // Combat Phase
        // ────────────────────────────────────────────────

        /// <summary>
        /// Combat Phase: 시간 재개, 우주선 조작 활성화, 적 스폰 시작.
        /// </summary>
        private void HandleEnterCombatPhase()
        {
            Time.timeScale = 1f;
            _healthSystem.Initialize();
            _shipController?.EnableControl();
            _waveManager.StartCurrentWave();
        }

        // ────────────────────────────────────────────────
        // Wave Result
        // ────────────────────────────────────────────────

        /// <summary>
        /// WaveManager에서 웨이브 클리어 이벤트를 받았을 때 호출됩니다.
        /// </summary>
        private void HandleWaveCleared(int waveNumber)
        {
            _lastClearedWave = waveNumber;
            _currentState = GameState.WaveResult;
            OnGameStateChanged?.Invoke(GameState.WaveResult);

            Time.timeScale = 0f;
            _shipController?.DisableControl();

            // 마지막 웨이브를 클리어했으면 게임 클리어
            if (_waveManager.IsAllWavesCleared)
            {
                HandleGameClear();
                return;
            }

            OnWaveCleared?.Invoke(waveNumber);

            // 카드 선택으로 이동
            ChangeState(GameState.CardSelection);
        }

        /// <summary>
        /// 우주선 체력이 0이 되었을 때 HealthSystem에서 이벤트를 받아 호출됩니다.
        /// </summary>
        private void HandleShipDied()
        {
            if (_currentState != GameState.CombatPhase) return;

            _currentState = GameState.WaveResult;
            OnGameStateChanged?.Invoke(GameState.WaveResult);

            Time.timeScale = 0f;
            _shipController?.DisableControl();
            _waveManager.StopCurrentWave();

            OnWaveFailed?.Invoke();

            // 스냅샷 복원 후 이전 웨이브로 복귀
            RestoreWaveSnapshot();
            _waveManager.RevertToPreviousWave();

            ChangeState(GameState.BuildPhase);
        }

        // ────────────────────────────────────────────────
        // Card Selection
        // ────────────────────────────────────────────────

        /// <summary>
        /// 카드 선택 단계 진입: 카드 풀에서 랜덤으로 cardChoiceCount장을 제시합니다.
        /// DeckManager나 카드 풀이 없으면 바로 BuildPhase로 복귀합니다.
        /// </summary>
        private void HandleEnterCardSelection()
        {
            if (_deckManager == null || _cardPool.Count == 0)
            {
                ChangeState(GameState.BuildPhase);
                return;
            }

            int count = _config != null ? _config.CardChoiceCount : 3;
            var choices = GetRandomCardChoices(count);
            _deckManager.ShowCardSelection(choices);
        }

        /// <summary>
        /// 플레이어가 카드를 선택했을 때 CardSelectionUI에서 호출합니다.
        /// </summary>
        public void OnCardSelected(CardData card)
        {
            Debug.Log($"[GameManager] OnCardSelected: card={card?.name}, currentState={_currentState}");
            if (_currentState != GameState.CardSelection)
            {
                Debug.LogWarning($"[GameManager] OnCardSelected 거부됨 - 현재 상태가 CardSelection이 아님: {_currentState}");
                return;
            }

            _deckManager.AddCard(card);
            Debug.Log($"[GameManager] AddCard 완료 - 덱 크기: {_deckManager.Deck.Count}");
            ChangeState(GameState.BuildPhase);
        }

        // ────────────────────────────────────────────────
        // 스냅샷
        // ────────────────────────────────────────────────

        /// <summary>
        /// 웨이브 시작 직전 게임 상태를 스냅샷으로 저장합니다.
        /// </summary>
        private void SaveWaveSnapshot()
        {
            _lastWaveSnapshot = new WaveSnapshot
            {
                waveNumber = _waveManager.CurrentWaveNumber,
                nodes = _shipGrid.SerializeNodes(),
                connections = _powerGraph.SerializeConnections(),
                deckCardNames = _deckManager != null ? _deckManager.GetCardNames() : new List<string>(),
                gold = _goldSystem != null ? _goldSystem.CurrentGold : 0
            };
        }

        /// <summary>
        /// 저장된 스냅샷으로 게임 상태를 복원합니다.
        /// </summary>
        private void RestoreWaveSnapshot()
        {
            if (_lastWaveSnapshot == null) return;

            _defaultShipSetup.RestoreFromSnapshot(_lastWaveSnapshot.nodes);
            _powerGraph.Clear();
            RestorePowerConnections(_lastWaveSnapshot.connections);

            _deckManager?.RestoreFromSnapshot(_lastWaveSnapshot.deckCardNames);
            _goldSystem?.SetGold(_lastWaveSnapshot.gold);
        }

        /// <summary>
        /// 직렬화된 동력 연결 데이터를 PowerGraph에 복원합니다.
        /// 그리드 좌표로 노드를 찾아 연결을 재생성합니다.
        /// </summary>
        private void RestorePowerConnections(List<PowerConnectionData> connections)
        {
            if (connections == null) return;

            foreach (var conn in connections)
            {
                var fromPos = new Vector2Int(conn.fromGridX, conn.fromGridY);
                var toPos = new Vector2Int(conn.toGridX, conn.toGridY);

                PlacedNode from = _shipGrid.GetNodeAt(fromPos);
                PlacedNode to = _shipGrid.GetNodeAt(toPos);

                if (from != null && to != null)
                    _powerGraph.TryAddConnection(from, to);
            }
        }

        // ────────────────────────────────────────────────
        // 게임 클리어 / 재시작
        // ────────────────────────────────────────────────

        private void HandleGameClear()
        {
            Time.timeScale = 0f;
            _shipController?.DisableControl();
            OnGameCleared?.Invoke();
        }

        /// <summary>
        /// 게임을 처음부터 재시작합니다. 게임오버 UI의 재시작 버튼에서 호출합니다.
        /// </summary>
        public void RestartGame()
        {
            _shipGrid.Clear();
            _powerGraph.Clear();
            _enemySpawner.StopWave();
            Time.timeScale = 1f;

            ChangeState(GameState.Init);
        }

        // ────────────────────────────────────────────────
        // 헬퍼
        // ────────────────────────────────────────────────

        /// <summary>
        /// 카드 풀에서 중복 없이 랜덤으로 count장을 뽑아서 반환합니다.
        /// </summary>
        private List<CardData> GetRandomCardChoices(int count)
        {
            var pool = new List<CardData>(_cardPool);
            var choices = new List<CardData>();

            count = Mathf.Min(count, pool.Count);

            for (int i = 0; i < count; i++)
            {
                int idx = UnityEngine.Random.Range(0, pool.Count);
                choices.Add(pool[idx]);
                pool.RemoveAt(idx);
            }

            return choices;
        }
    }
}
