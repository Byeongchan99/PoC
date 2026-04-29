using UnityEngine;
using UnityEngine.UI;
using TMPro;
using POC5.Data;

namespace POC5.UI
{
    /// <summary>
    /// 스피릿 카드 UI를 담당한다.
    /// SpiritCard 프리팹에 이 컴포넌트를 붙이고
    /// Inspector에서 각 텍스트·이미지 참조를 연결한다.
    ///
    /// 카드의 시각적 레이아웃은 프리팹 에디터에서 수정한다.
    /// 이 스크립트는 SpiritData 바인딩만 담당한다.
    /// </summary>
    public class SpiritCardView : MonoBehaviour
    {
        [Header("카드 내부 UI 참조 (프리팹에서 연결)")]
        [Tooltip("스피릿 이름 텍스트.")]
        [SerializeField] private TextMeshProUGUI _nameText;

        [Tooltip("스피릿 속성(원소) 텍스트.")]
        [SerializeField] private TextMeshProUGUI _elementText;

        [Tooltip("작업 능력치 텍스트.")]
        [SerializeField] private TextMeshProUGUI _workPowerText;

        [Tooltip("스피릿 아이콘 이미지.")]
        [SerializeField] private Image _iconImage;

        /// <summary>이 카드가 표시하는 스피릿 데이터.</summary>
        public SpiritData Data { get; private set; }

        /// <summary>
        /// 스피릿 데이터를 카드 UI에 바인딩한다.
        /// GameSceneManager에서 Instantiate 직후 호출한다.
        /// </summary>
        public void Initialize(SpiritData data)
        {
            Data = data;

            if (_nameText != null)
                _nameText.text = data.DisplayName;

            if (_elementText != null)
                _elementText.text = data.Element.ToString();

            if (_workPowerText != null)
                _workPowerText.text = $"Work  {data.WorkPower:F1}";

            if (_iconImage != null && data.Icon != null)
            {
                _iconImage.sprite = data.Icon;
                _iconImage.preserveAspect = true;
            }
        }
    }
}
