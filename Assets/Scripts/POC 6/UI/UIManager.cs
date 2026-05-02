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

        [Header("웨이브 실패 UI")]
        [Tooltip("웨이브 실패 시 표시됩니다. 닫으면 스냅샷이 복원된 빌드 페이즈가 그대로 진행됩니다.")]
        [SerializeField] private GameObject _waveFailedPanel;
        [Tooltip("웨이브 실패 패널의 닫기 버튼. 클릭 시 패널만 숨기고 빌드 페이즈를 계속 진행합니다.")]
        [SerializeField] private Button _waveFailedCloseButton;

        [Header("게임 클리어 UI")]
        [Tooltip("모든 웨이브 클리어 시 표시됩니다.")]
        [SerializeField] private GameObject _gameClearPanel;

        private void Awake()
        {
            if (_startWaveButton != null)
                _startWaveButton.onClick.AddListener(HandleStartWaveClicked);

            // 웨이브 실패 닫기 버튼: 패널만 닫고 이미 복원된 빌드 페이즈를 이어서 진행
            if (_waveFailedCloseButton != null)
                _waveFailedCloseButton.onClick.AddListener(HandleWaveFailedCloseClicked);

            if (_waveFailedPanel != null) _waveFailedPanel.SetActive(false);
            if (_gameClearPanel != null) _gameClearPanel.SetActive(false);
        }

        private void OnEnable()
        {
            HealthSystem.OnHealthChanged += RefreshHP;
            GoldSystem.OnGoldChanged += RefreshGold;
            GameManager.OnGameStateChanged += RefreshPhaseUI;
            WaveManager.OnWaveStarted += RefreshWaveNumber;
            GameManager.OnWaveFailed += ShowWaveFailed;
            GameManager.OnGameCleared += ShowGameClear;
        }

        private void OnDisable()
        {
            HealthSystem.OnHealthChanged -= RefreshHP;
            GoldSystem.OnGoldChanged -= RefreshGold;
            GameManager.OnGameStateChanged -= RefreshPhaseUI;
            WaveManager.OnWaveStarted -= RefreshWaveNumber;
            GameManager.OnWaveFailed -= ShowWaveFailed;
            GameManager.OnGameCleared -= ShowGameClear;
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
        /// 웨이브 실패 시 실패 패널을 표시합니다.
        /// GameManager가 이미 스냅샷을 복원하고 빌드 페이즈로 전환한 상태이므로
        /// 패널을 닫기만 하면 바로 이어서 플레이할 수 있습니다.
        /// </summary>
        private void ShowWaveFailed()
        {
            if (_waveFailedPanel != null) _waveFailedPanel.SetActive(true);
        }

        /// <summary>
        /// 모든 웨이브 클리어 시 게임 클리어 패널을 표시합니다.
        /// </summary>
        private void ShowGameClear()
        {
            if (_gameClearPanel != null) _gameClearPanel.SetActive(true);
        }

        // ────────────────────────────────────────────────
        // 버튼 핸들러
        // ────────────────────────────────────────────────

        private void HandleStartWaveClicked()
        {
            GameManager.Instance?.OnStartWaveButtonPressed();
        }

        /// <summary>
        /// 웨이브 실패 패널의 닫기 버튼 핸들러입니다.
        /// 패널만 닫으면 됩니다. GameManager는 이미 빌드 페이즈 상태입니다.
        /// </summary>
        private void HandleWaveFailedCloseClicked()
        {
            if (_waveFailedPanel != null) _waveFailedPanel.SetActive(false);
        }
    }
}
