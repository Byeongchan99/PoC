using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace POC6
{
    /// <summary>
    /// 인게임 HUD 전체를 관리합니다.
    /// HP 바, 골드 표시, 웨이브 번호, 페이즈 전환 UI를 담당합니다.
    /// 각 시스템의 이벤트를 구독해서 자동으로 UI를 갱신합니다.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("HP UI")]
        [SerializeField] private Slider _hpSlider;
        [SerializeField] private TextMeshProUGUI _hpText;

        [Header("골드 UI")]
        [SerializeField] private TextMeshProUGUI _goldText;

        [Header("웨이브 UI")]
        [SerializeField] private TextMeshProUGUI _waveText;

        [Header("페이즈 UI")]
        [Tooltip("Build Phase에서만 표시되는 UI 그룹")]
        [SerializeField] private GameObject _buildPhaseUI;

        [Tooltip("Combat Phase에서만 표시되는 UI 그룹")]
        [SerializeField] private GameObject _combatPhaseUI;

        [Header("버튼")]
        [Tooltip("다음 웨이브 시작 버튼 (Build Phase에서 표시)")]
        [SerializeField] private Button _startWaveButton;

        [Header("게임오버 UI")]
        [SerializeField] private GameObject _gameOverPanel;
        [SerializeField] private Button _restartButton;

        private void Awake()
        {
            if (_startWaveButton != null) _startWaveButton.onClick.AddListener(HandleStartWaveClicked);
            if (_restartButton != null) _restartButton.onClick.AddListener(HandleRestartClicked);
            if (_gameOverPanel != null) _gameOverPanel.SetActive(false);
        }

        private void OnEnable()
        {
            HealthSystem.OnHealthChanged += RefreshHP;
            GoldSystem.OnGoldChanged += RefreshGold;
            GameManager.OnGameStateChanged += RefreshPhaseUI;
            WaveManager.OnWaveStarted += RefreshWaveNumber;
            HealthSystem.OnDied += ShowGameOver;
        }

        private void OnDisable()
        {
            HealthSystem.OnHealthChanged -= RefreshHP;
            GoldSystem.OnGoldChanged -= RefreshGold;
            GameManager.OnGameStateChanged -= RefreshPhaseUI;
            WaveManager.OnWaveStarted -= RefreshWaveNumber;
            HealthSystem.OnDied -= ShowGameOver;
        }

        // ────────────────────────────────────────────────
        // UI 갱신 메서드들
        // ────────────────────────────────────────────────

        /// <summary>
        /// HP 바와 텍스트를 갱신합니다.
        /// </summary>
        private void RefreshHP(float current, float max)
        {
            if (_hpSlider != null)
            {
                _hpSlider.maxValue = max;
                _hpSlider.value = current;
            }

            if (_hpText != null)
                _hpText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        /// <summary>
        /// 골드 텍스트를 갱신합니다.
        /// </summary>
        private void RefreshGold(int gold)
        {
            if (_goldText != null)
                _goldText.text = $"Gold: {gold}";
        }

        /// <summary>
        /// 웨이브 번호 텍스트를 갱신합니다.
        /// </summary>
        private void RefreshWaveNumber(int waveNumber)
        {
            if (_waveText != null)
                _waveText.text = $"Wave {waveNumber}";
        }

        /// <summary>
        /// 게임 상태에 따라 표시할 UI 그룹을 전환합니다.
        /// </summary>
        private void RefreshPhaseUI(GameState state)
        {
            bool isBuild = state == GameState.BuildPhase;
            bool isCombat = state == GameState.CombatPhase;

            // Unity SerializedField는 ?.가 fake-null을 처리하지 못하므로 명시적 null 체크 사용
            if (_buildPhaseUI != null) _buildPhaseUI.SetActive(isBuild);
            if (_combatPhaseUI != null) _combatPhaseUI.SetActive(isCombat);
            if (_startWaveButton != null) _startWaveButton.gameObject.SetActive(isBuild);
        }

        /// <summary>
        /// 우주선이 파괴되면 게임오버 패널을 표시합니다.
        /// </summary>
        private void ShowGameOver()
        {
            if (_gameOverPanel != null) _gameOverPanel.SetActive(true);
        }

        // ────────────────────────────────────────────────
        // 버튼 핸들러
        // ────────────────────────────────────────────────

        private void HandleStartWaveClicked()
        {
            GameManager.Instance?.OnStartWaveButtonPressed();
        }

        private void HandleRestartClicked()
        {
            if (_gameOverPanel != null) _gameOverPanel.SetActive(false);
            if (GameManager.Instance != null) GameManager.Instance.RestartGame();
        }
    }
}
