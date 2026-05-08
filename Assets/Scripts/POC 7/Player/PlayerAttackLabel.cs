using TMPro;
using UnityEngine;

namespace POC7
{
    /// <summary>
    /// 플레이어 위에 현재 공격력을 표시하는 레이블 컴포넌트.
    /// PlayerCombat과 같은 GameObject에 부착해야 한다.
    ///
    /// [주의]
    /// 캔버스를 플레이어의 자식으로 두면 플레이어 scale이 그대로 상속되어
    /// 텍스트 크기가 의도치 않게 줄어든다. EnemyHealthHUD와 동일하게
    /// 캔버스를 독립 오브젝트로 생성하고 LateUpdate에서 위치를 추적한다.
    /// </summary>
    public class PlayerAttackLabel : MonoBehaviour
    {
        /// <summary>플레이어 중심 기준 레이블의 Y축 오프셋.</summary>
        [SerializeField] private float _labelYOffset = 0.6f;

        /// <summary>텍스트 폰트 크기. Canvas scale 0.01 기준 36 → 약 0.36 world unit.</summary>
        [SerializeField] private float _fontSize = 36f;

        [SerializeField] private Color _labelColor = Color.yellow;

        private PlayerCombat _playerCombat;
        private TMP_Text _label;

        /// <summary>
        /// PlayerCombat 참조를 가져오고, 씬 루트에 독립 World Space Canvas를 생성한다.
        /// 플레이어 scale에 영향받지 않도록 캔버스를 자식이 아닌 루트 오브젝트로 배치한다.
        /// </summary>
        private void Awake()
        {
            _playerCombat = GetComponent<PlayerCombat>();

            GameObject canvasGo = new GameObject("AttackLabelCanvas");
            // 씬 루트에 배치하여 플레이어 scale 상속을 차단한다.
            canvasGo.transform.SetParent(null);
            // Canvas scale 0.01: fontSize 36 → 실제 크기 0.36 world unit
            canvasGo.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;

            GameObject textGo = new GameObject("AttackText");
            textGo.transform.SetParent(canvasGo.transform, false);

            _label = textGo.AddComponent<TextMeshProUGUI>();
            _label.alignment = TextAlignmentOptions.Center;
            _label.fontSize = _fontSize;
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
        /// 플레이어 이동이 모두 반영된 후 레이블 위치를 갱신한다.
        /// </summary>
        private void LateUpdate()
        {
            if (_label == null)
                return;

            _label.transform.position = transform.position + Vector3.up * _labelYOffset;
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
