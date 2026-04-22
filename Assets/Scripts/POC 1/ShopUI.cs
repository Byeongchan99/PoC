using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace POC1
{
    public class ShopUI : MonoBehaviour
    {
        [SerializeField] SwordStats swordStats;
        [SerializeField] TextMeshProUGUI goldText;
        [SerializeField] Button[] upgradeButtons;    // [0]=공격력 [1]=이동속도 [2]=회전속도
        [SerializeField] TextMeshProUGUI[] costTexts;
        [SerializeField] TextMeshProUGUI[] statTexts; // [0]=공격력 [1]=이동속도 [2]=회전속도

        static readonly float[] Increments  = { 5f,  1f,  2f  };
        static readonly int[]   BaseCosts   = { 10,  15,  12  };
        static readonly float[] Multipliers = { 2f, 1.8f, 1.8f };

        readonly int[] _levels = { 0, 0, 0 };

        void Awake()
        {
            for (int i = 0; i < upgradeButtons.Length; i++)
            {
                int idx = i;
                upgradeButtons[i].onClick.AddListener(() => TryUpgrade(idx));
            }
        }

        void Start()
        {
            GameManager.Instance.OnGoldChanged += RefreshUI;
            RefreshUI(GameManager.Instance.Gold);
        }

        void OnDestroy() => GameManager.Instance.OnGoldChanged -= RefreshUI;

        void TryUpgrade(int idx)
        {
            int cost = GetCost(idx);
            if (!GameManager.Instance.SpendGold(cost)) return;

            _levels[idx]++;
            switch (idx)
            {
                case 0: swordStats.attackDamage  += Increments[0]; break;
                case 1: swordStats.moveSpeed     += Increments[1]; break;
                case 2: swordStats.rotationSpeed += Increments[2]; break;
            }
            RefreshUI(GameManager.Instance.Gold);
        }

        int GetCost(int idx) =>
            Mathf.RoundToInt(BaseCosts[idx] * Mathf.Pow(Multipliers[idx], _levels[idx]));

        void RefreshUI(int gold)
        {
            goldText.text = $"Gold: {gold}";
            for (int i = 0; i < costTexts.Length; i++)
                costTexts[i].text = $"{GetCost(i)}G";

            if (statTexts != null && statTexts.Length >= 3)
            {
                statTexts[0].text = $"{swordStats.attackDamage:F1}";
                statTexts[1].text = $"{swordStats.moveSpeed:F1}";
                statTexts[2].text = $"{swordStats.rotationSpeed:F1}";
            }
        }
    }
}
