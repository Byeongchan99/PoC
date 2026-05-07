using TMPro;
using UnityEngine;

namespace POC7
{
    /// <summary>
    /// 플레이어의 현재 킬 카운트와 다음 공격력 배수까지의 진행도를 텍스트로 표시하는 UI 컴포넌트.
    /// 이 스크립트가 부착된 GameObject에 TMP_Text 컴포넌트도 함께 있어야 한다.
    ///
    /// [씬 설정]
    /// 1. Canvas 하위에 TextMeshProUGUI를 배치한다.
    /// 2. 해당 GameObject에 이 스크립트를 부착한다.
    /// 3. Inspector에서 PlayerCombat 참조를 연결한다.
    ///
    /// 표시 형식: "현재킬 / 목표킬" (예: "12 / 16")
    /// 공격력이 배수로 오르면 목표 킬 수도 함께 갱신된다.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class KillCountUI : MonoBehaviour
    {
        [SerializeField] private PlayerCombat _playerCombat;

        private TMP_Text _text;

        /// <summary>
        /// TMP_Text 컴포넌트 참조를 캐시한다.
        /// </summary>
        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        /// <summary>
        /// 오브젝트 활성화 시 킬 카운트 변경 이벤트를 구독한다.
        /// </summary>
        private void OnEnable()
        {
            if (_playerCombat != null)
                _playerCombat.OnKillCountChanged += UpdateText;
        }

        /// <summary>
        /// 모든 Awake가 완료된 후 초기값을 표시한다.
        /// </summary>
        private void Start()
        {
            if (_playerCombat != null)
                UpdateText(_playerCombat.KillCount, _playerCombat.CurrentAttackPower);
        }

        /// <summary>
        /// 오브젝트 비활성화 시 이벤트 구독을 해제한다.
        /// </summary>
        private void OnDisable()
        {
            if (_playerCombat != null)
                _playerCombat.OnKillCountChanged -= UpdateText;
        }

        /// <summary>
        /// 킬 카운트 텍스트를 갱신한다. 형식: "현재킬 / 목표킬"
        /// </summary>
        private void UpdateText(int current, int target)
        {
            if (_text != null)
                _text.text = $"{current} / {target}";
        }
    }
}
