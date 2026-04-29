using System;
using UnityEngine;

namespace POC5.Runtime
{
    /// <summary>
    /// 골드(재화) 잔액을 관리하는 씬 컴포넌트.
    ///
    /// 잔액이 바뀔 때마다 OnGoldChanged 이벤트를 발행하므로
    /// CurrencyView 등 UI 컴포넌트가 이벤트만 구독하면 된다.
    ///
    /// 사용법: 씬에 빈 게임 오브젝트를 만들고 이 컴포넌트를 붙인다.
    /// </summary>
    public class CurrencySystem : MonoBehaviour
    {
        [Tooltip("씬 시작 시 지급되는 초기 골드.")]
        [SerializeField] private int _initialGold = 500;

        /// <summary>현재 골드 잔액.</summary>
        public int CurrentGold { get; private set; }

        /// <summary>잔액이 바뀔 때 발행된다. 인자는 변경 후 잔액이다.</summary>
        public event Action<int> OnGoldChanged;

        private void Awake()
        {
            CurrentGold = _initialGold;
        }

        /// <summary>
        /// 주어진 금액을 지불할 수 있는지 확인한다.
        /// </summary>
        public bool CanAfford(int amount) => CurrentGold >= amount;

        /// <summary>
        /// 골드를 지불한다. 잔액이 부족하면 false를 반환하고 잔액은 변하지 않는다.
        /// </summary>
        public bool TrySpend(int amount)
        {
            if (amount <= 0 || !CanAfford(amount)) return false;
            CurrentGold -= amount;
            OnGoldChanged?.Invoke(CurrentGold);
            return true;
        }

        /// <summary>
        /// 골드를 획득한다. 시장에서 자원을 판매할 때 등 수익 발생 시 호출한다.
        /// </summary>
        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            CurrentGold += amount;
            OnGoldChanged?.Invoke(CurrentGold);
        }
    }
}
