using UnityEngine;
using UnityEngine.UI;

namespace POC8
{
    /// <summary>
    /// SaturationSystem의 현재 적 수를 슬라이더로 표시하는 UI 컴포넌트.
    /// 이 스크립트가 부착된 GameObject에 Slider 컴포넌트도 함께 있어야 한다.
    ///
    /// [씬 설정]
    /// 1. Screen Space - Overlay Canvas 하위에 Slider UI를 배치한다.
    /// 2. Slider GameObject에 이 스크립트를 부착한다.
    /// 3. Inspector에서 SaturationSystem 참조를 연결한다.
    /// 4. Slider의 Interactable 체크를 해제한다 (플레이어가 조작하지 않는 표시 전용 슬라이더).
    ///
    /// [실무 권장]
    /// POC 단계에서는 Slider로 충분하다.
    /// 실제 서비스에서는 Shader Graph로 커스텀 게이지 머티리얼을 만들어
    /// 색상 그라디언트나 펄스 애니메이션을 추가하면 더 풍부한 표현이 가능하다.
    /// </summary>
    [RequireComponent(typeof(Slider))]
    public class SaturationUI : MonoBehaviour
    {
        [SerializeField] private SaturationSystem _saturationSystem;

        private Slider _slider;

        /// <summary>
        /// Slider를 표시 전용 모드로 초기화한다.
        /// </summary>
        private void Awake()
        {
            _slider = GetComponent<Slider>();
            _slider.minValue = 0f;
            _slider.maxValue = 1f;
            _slider.interactable = false;
        }

        /// <summary>
        /// 오브젝트 활성화 시 포화도 변경 이벤트를 구독한다.
        /// </summary>
        private void OnEnable()
        {
            if (_saturationSystem != null)
                _saturationSystem.OnSaturationChanged += UpdateSlider;
        }

        /// <summary>
        /// 모든 Awake가 완료된 후 초기 포화도 비율을 표시한다.
        /// </summary>
        private void Start()
        {
            if (_saturationSystem != null)
                _slider.value = _saturationSystem.SaturationRatio;
        }

        /// <summary>
        /// 오브젝트 비활성화 시 이벤트 구독을 해제한다.
        /// </summary>
        private void OnDisable()
        {
            if (_saturationSystem != null)
                _saturationSystem.OnSaturationChanged -= UpdateSlider;
        }

        /// <summary>
        /// 포화도 비율(0~1)로 슬라이더 값을 갱신한다.
        /// </summary>
        private void UpdateSlider(int current, int max)
        {
            if (_slider != null)
                _slider.value = max > 0 ? (float)current / max : 0f;
        }
    }
}
