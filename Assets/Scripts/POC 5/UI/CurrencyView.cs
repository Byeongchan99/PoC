using UnityEngine;
using TMPro;
using POC5.Runtime;

namespace POC5.UI
{
    /// <summary>
    /// HUD에 현재 골드 잔액을 표시하는 컴포넌트.
    /// CurrencySystem의 OnGoldChanged 이벤트를 구독해 잔액이 바뀔 때마다 텍스트를 갱신한다.
    ///
    /// 사용법: HUD GameObject에 이 컴포넌트를 붙이고
    ///         Inspector에서 CurrencySystem과 텍스트 컴포넌트를 연결한다.
    /// </summary>
    public class CurrencyView : MonoBehaviour
    {
        [Header("씬 참조")]
        [Tooltip("잔액 변경 이벤트를 발행하는 CurrencySystem.")]
        [SerializeField] private CurrencySystem _currencySystem;

        [Header("UI 참조 (프리팹에서 연결)")]
        [Tooltip("골드 잔액을 표시할 텍스트 컴포넌트.")]
        [SerializeField] private TextMeshProUGUI _goldText;

        [Tooltip("잔액 표시 포맷. {0} 위치에 숫자가 들어간다.")]
        [SerializeField] private string _format = "{0} G";

        private void Awake()
        {
            _currencySystem.OnGoldChanged += UpdateDisplay;
        }

        private void Start()
        {
            // 씬 시작 시 초기 잔액을 즉시 표시한다
            UpdateDisplay(_currencySystem.CurrentGold);
        }

        private void OnDestroy()
        {
            _currencySystem.OnGoldChanged -= UpdateDisplay;
        }

        /// <summary>
        /// 텍스트를 새 잔액으로 갱신한다. OnGoldChanged 이벤트 콜백.
        /// </summary>
        private void UpdateDisplay(int gold)
        {
            if (_goldText != null)
                _goldText.text = string.Format(_format, gold);
        }
    }
}
