using UnityEngine;
using POC5.Data;
using POC5.Graph;

namespace POC5.Runtime
{
    /// <summary>
    /// 시장 노드의 판매 이벤트를 받아 CurrencySystem에 골드를 추가하는 컴포넌트.
    ///
    /// 동작 흐름:
    ///   1. GameSceneManager가 Market 타입 설비를 생성할 때 RegisterMarketNode()를 호출한다.
    ///   2. 이후 FacilityGraphNode.TryConvertToMoney()가 실행될 때마다
    ///      OnResourceSold 이벤트가 발행된다.
    ///   3. HandleSale()이 ResourceData 배열에서 해당 자원의 판매 가격을 조회하고
    ///      CurrencySystem.AddGold()를 호출한다.
    ///
    /// 사용법:
    ///   씬에 빈 GameObject를 만들고 이 컴포넌트를 붙인다.
    ///   Inspector에서 CurrencySystem과 ResourcePrices 배열을 연결한다.
    ///   ResourcePrices에는 판매 가격이 설정된 ResourceData SO를 등록한다.
    /// </summary>
    public class MarketSalesHandler : MonoBehaviour
    {
        [Header("씬 참조")]
        [Tooltip("골드를 추가할 CurrencySystem.")]
        [SerializeField] private CurrencySystem _currencySystem;

        [Header("자원 판매 가격표")]
        [Tooltip("자원별 판매 가격 정보를 담은 ResourceData SO 목록.\n" +
                 "시장에서 처리하는 자원 종류마다 한 항목씩 등록한다.")]
        [SerializeField] private ResourceData[] _resourcePrices;

        /// <summary>
        /// 시장 노드의 판매 이벤트를 구독한다.
        /// GameSceneManager가 Market 타입 설비를 생성할 때 호출한다.
        /// </summary>
        public void RegisterMarketNode(FacilityGraphNode marketNode)
        {
            if (marketNode == null) return;
            marketNode.OnResourceSold += HandleSale;
        }

        /// <summary>
        /// 판매 이벤트 콜백. 자원 가격을 조회해 골드를 추가한다.
        /// </summary>
        private void HandleSale(ResourceType resourceType, int amount)
        {
            int pricePerUnit = LookupPrice(resourceType);
            int gold         = pricePerUnit * amount;
            _currencySystem.AddGold(gold);

            Debug.Log($"[MarketSalesHandler] {resourceType} x{amount} 판매 → +{gold}G " +
                      $"(현재 잔액: {_currencySystem.CurrentGold}G)");
        }

        /// <summary>
        /// _resourcePrices 배열에서 해당 자원 종류의 판매 가격을 반환한다.
        /// 등록되지 않은 자원이면 1을 반환한다.
        /// </summary>
        private int LookupPrice(ResourceType resourceType)
        {
            if (_resourcePrices == null) return 1;
            foreach (var data in _resourcePrices)
                if (data != null && data.ResourceType == resourceType)
                    return data.SellPrice;
            return 1;
        }
    }
}
