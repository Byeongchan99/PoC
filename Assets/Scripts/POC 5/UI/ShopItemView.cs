using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using POC5.Data;

namespace POC5.UI
{
    /// <summary>
    /// 상점 패널 내 설비 한 항목의 UI를 담당한다.
    /// ShopItemView 프리팹에 이 컴포넌트를 붙이고
    /// Inspector에서 텍스트·이미지·버튼 참조를 연결한다.
    /// </summary>
    public class ShopItemView : MonoBehaviour
    {
        [Header("UI 참조 (프리팹에서 연결)")]
        [Tooltip("설비 이름 텍스트.")]
        [SerializeField] private TextMeshProUGUI _nameText;

        [Tooltip("구매 가격 텍스트.")]
        [SerializeField] private TextMeshProUGUI _priceText;

        [Tooltip("설비 아이콘 이미지.")]
        [SerializeField] private Image _iconImage;

        [Tooltip("구매 버튼.")]
        [SerializeField] private Button _buyButton;

        private FacilityData _data;
        private Action<FacilityData> _onBuy;

        /// <summary>
        /// 설비 데이터를 바인딩하고 구매 콜백을 등록한다.
        /// ShopPanel에서 아이템을 생성한 직후 호출한다.
        /// </summary>
        public void Initialize(FacilityData data, Action<FacilityData> onBuy, bool canAfford)
        {
            _data  = data;
            _onBuy = onBuy;

            if (_nameText != null)
                _nameText.text = data.DisplayName;

            if (_priceText != null)
                _priceText.text = $"{data.PurchasePrice} G";

            if (_iconImage != null && data.Icon != null)
            {
                _iconImage.sprite = data.Icon;
                _iconImage.preserveAspect = true;
            }

            if (_buyButton != null)
            {
                _buyButton.onClick.RemoveAllListeners();
                _buyButton.onClick.AddListener(OnBuyClicked);
            }

            SetAffordable(canAfford);
        }

        /// <summary>
        /// 골드 잔액 변화에 따라 구매 버튼 활성화 여부를 갱신한다.
        /// ShopPanel에서 잔액이 바뀔 때마다 호출한다.
        /// </summary>
        public void SetAffordable(bool canAfford)
        {
            if (_buyButton != null)
                _buyButton.interactable = canAfford;
        }

        /// <summary>구매 버튼 클릭 시 상위 콜백을 호출한다.</summary>
        private void OnBuyClicked()
        {
            _onBuy?.Invoke(_data);
        }
    }
}
