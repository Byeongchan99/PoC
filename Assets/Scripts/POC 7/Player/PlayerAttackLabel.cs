using TMPro;
using UnityEngine;

namespace POC7
{
    /// <summary>
    /// 플레이어 위에 현재 공격력을 표시하는 레이블 컴포넌트.
    /// PlayerCombat과 같은 GameObject에 부착해야 한다.
    /// </summary>
    public class PlayerAttackLabel : MonoBehaviour
    {
        [SerializeField] private float _labelYOffset = 0.6f;
        [SerializeField] private Color _labelColor = Color.yellow;

        private PlayerCombat _playerCombat;
        private TMP_Text _label;

        /// <summary>
        /// PlayerCombat 참조를 가져오고 World Space Canvas + TMP_Text를 동적으로 생성한다.
        /// </summary>
        private void Awake()
        {
            _playerCombat = GetComponent<PlayerCombat>();

            GameObject canvasGo = new GameObject("AttackLabelCanvas");
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, _labelYOffset, 0f);
            // EnemyHealthHUD와 동일한 scale 규칙: 0.01 → fontSize 36 = 0.36 world unit
            canvasGo.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;

            GameObject textGo = new GameObject("AttackText");
            textGo.transform.SetParent(canvasGo.transform, false);

            _label = textGo.AddComponent<TextMeshProUGUI>();
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = 36f;
            _label.color = _labelColor;
            _label.enableWordWrapping = false;

            RectTransform rt = textGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 80f);
            rt.localPosition = Vector3.zero;
        }

        /// <summary>
        /// 오브젝트 활성화 시 공격력 변경 이벤트를 구독한다.
        /// </summary>
        private void OnEnable()
        {
            _playerCombat.OnAttackPowerChanged += UpdateLabel;
        }

        /// <summary>
        /// 모든 Awake가 완료된 후 초기값을 표시한다.
        /// OnEnable에서 읽으면 PlayerCombat.Awake 실행 순서에 따라 0이 반환될 수 있다.
        /// </summary>
        private void Start()
        {
            UpdateLabel(_playerCombat.CurrentAttackPower);
        }

        /// <summary>
        /// 오브젝트 비활성화 시 이벤트 구독을 해제한다.
        /// </summary>
        private void OnDisable()
        {
            _playerCombat.OnAttackPowerChanged -= UpdateLabel;
        }

        /// <summary>
        /// 공격력 수치를 레이블에 반영한다.
        /// </summary>
        private void UpdateLabel(int attackPower)
        {
            if (_label != null)
                _label.text = $"{attackPower}";
        }
    }
}
