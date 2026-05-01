using System;
using UnityEngine;

namespace POC6
{
    /// <summary>
    /// 골드(재화)를 관리합니다.
    /// 적 처치 시 골드 이벤트를 받아 자동으로 추가하고, 노드 업그레이드 비용을 처리합니다.
    /// </summary>
    public class GoldSystem : MonoBehaviour
    {
        /// <summary>골드가 변경될 때 발행됩니다. (현재 골드 양)</summary>
        public static event Action<int> OnGoldChanged;

        [Header("디버그 (읽기 전용)")]
        [SerializeField] private int _currentGold;

        public int CurrentGold => _currentGold;

        private void OnEnable()
        {
            Enemy.OnGoldDropped += AddGold;
        }

        private void OnDisable()
        {
            Enemy.OnGoldDropped -= AddGold;
        }

        /// <summary>
        /// 골드를 초기값으로 설정합니다. 게임 시작 시 GameManager에서 호출합니다.
        /// </summary>
        public void Initialize(int startingGold)
        {
            _currentGold = startingGold;
            OnGoldChanged?.Invoke(_currentGold);
        }

        /// <summary>
        /// 골드를 추가합니다. 적 처치 이벤트에서 자동으로 호출됩니다.
        /// </summary>
        public void AddGold(int amount)
        {
            if (amount <= 0) return;
            _currentGold += amount;
            OnGoldChanged?.Invoke(_currentGold);
        }

        /// <summary>
        /// 골드를 소비합니다. 성공 여부를 반환합니다. (잔액 부족 시 false)
        /// </summary>
        public bool SpendGold(int amount)
        {
            if (amount <= 0) return true;
            if (_currentGold < amount) return false;

            _currentGold -= amount;
            OnGoldChanged?.Invoke(_currentGold);
            return true;
        }

        /// <summary>
        /// 골드를 특정 값으로 직접 설정합니다. 스냅샷 복원 시 사용합니다.
        /// </summary>
        public void SetGold(int amount)
        {
            _currentGold = Mathf.Max(0, amount);
            OnGoldChanged?.Invoke(_currentGold);
        }

        /// <summary>
        /// 주어진 비용을 낼 수 있는지 확인합니다.
        /// </summary>
        public bool CanAfford(int amount) => _currentGold >= amount;
    }
}
