using UnityEngine;

namespace POC4
{
    /// <summary>
    /// 카드 제작에 사용하는 코스트(자원)를 관리하는 클래스.
    ///
    /// 코스트 누적 방식:
    ///   - 준비 페이즈마다 AddRoundCost() 를 외부(GameManager 등)에서 호출해 일정량을 추가한다.
    ///   - 사용하지 않은 코스트는 그대로 유지되어 다음 라운드로 이월된다.
    ///
    /// 7단계 GameManager 연동 시 AddRoundCost() 를 준비 페이즈 시작 시 호출하면 된다.
    /// POC에서는 ContextMenu 디버그 메서드로 수동 추가한다.
    /// </summary>
    public class CostManager : MonoBehaviour
    {
        // -------------------------------------------------------
        // Inspector 노출 필드
        // -------------------------------------------------------

        [Header("Cost Settings")]
        [Tooltip("게임 시작 시 보유할 초기 코스트")]
        [SerializeField] private int _initialCost = 0;

        [Tooltip("준비 페이즈마다 수급되는 코스트량")]
        [SerializeField] private int _costPerRound = 10;

        // -------------------------------------------------------
        // 내부 상태
        // -------------------------------------------------------

        private int _currentCost;

        // -------------------------------------------------------
        // 프로퍼티
        // -------------------------------------------------------

        /// <summary>현재 보유 코스트 (읽기 전용)</summary>
        public int CurrentCost => _currentCost;

        // -------------------------------------------------------
        // 유니티 생명주기
        // -------------------------------------------------------

        private void Awake()
        {
            _currentCost = _initialCost;
        }

        // -------------------------------------------------------
        // 코스트 수급 / 소비
        // -------------------------------------------------------

        /// <summary>
        /// 준비 페이즈마다 호출해 라운드 코스트를 추가한다.
        /// GameManager가 페이즈 전환 시 호출한다 (7단계).
        /// </summary>
        public void AddRoundCost()
        {
            AddCost(_costPerRound);
        }

        /// <summary>
        /// 지정한 양의 코스트를 추가한다.
        /// 음수나 0이 전달되면 무시한다.
        /// </summary>
        public void AddCost(int amount)
        {
            if (amount <= 0) return;
            _currentCost += amount;
            Debug.Log($"[CostManager] 코스트 +{amount} → 현재 보유: {_currentCost}");
        }

        /// <summary>
        /// 지정한 양의 코스트를 차감한다.
        /// 잔액이 부족하면 차감하지 않고 false를 반환한다.
        /// 반환값: 차감 성공 여부
        /// </summary>
        public bool SpendCost(int amount)
        {
            if (amount <= 0) return true;

            if (_currentCost < amount)
            {
                Debug.Log($"[CostManager] 코스트 부족 (보유: {_currentCost}, 필요: {amount})");
                return false;
            }

            _currentCost -= amount;
            Debug.Log($"[CostManager] 코스트 -{amount} → 현재 보유: {_currentCost}");
            return true;
        }

        // -------------------------------------------------------
        // Inspector ContextMenu (디버그)
        // -------------------------------------------------------

        /// <summary>
        /// 코스트를 한 라운드치(_costPerRound)만큼 즉시 추가하는 디버그 메서드.
        /// </summary>
        [ContextMenu("Debug: 라운드 코스트 추가")]
        private void DebugAddRoundCost()
        {
            AddRoundCost();
        }

        /// <summary>
        /// 코스트를 초기값으로 되돌리는 디버그 메서드.
        /// </summary>
        [ContextMenu("Debug: 코스트 초기화")]
        private void DebugResetCost()
        {
            _currentCost = _initialCost;
            Debug.Log($"[CostManager] 코스트 초기화 → 현재 보유: {_currentCost}");
        }
    }
}
