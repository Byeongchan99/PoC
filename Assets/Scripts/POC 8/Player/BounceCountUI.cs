using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace POC8
{
    /// <summary>
    /// 반사 횟수를 증가/감소 버튼으로 조절하고 현재 값을 텍스트로 표시하는 UI 컴포넌트.
    ///
    /// [씬 설정]
    /// 1. Canvas 하위에 적절히 UI를 배치한다.
    /// 2. 빈 GameObject에 이 스크립트를 부착한다.
    /// 3. Inspector에서 PlayerController, 증가 버튼, 감소 버튼, 텍스트를 연결한다.
    /// </summary>
    public class BounceCountUI : MonoBehaviour
    {
        [SerializeField] private PlayerController _playerController;

        /// <summary>반사 횟수를 1 증가시키는 버튼.</summary>
        [SerializeField] private Button _increaseButton;

        /// <summary>반사 횟수를 1 감소시키는 버튼.</summary>
        [SerializeField] private Button _decreaseButton;

        /// <summary>현재 반사 횟수를 표시하는 텍스트.</summary>
        [SerializeField] private TMP_Text _countText;

        /// <summary>
        /// 버튼 클릭 이벤트를 연결한다.
        /// </summary>
        private void OnEnable()
        {
            if (_increaseButton != null)
                _increaseButton.onClick.AddListener(OnIncreaseClicked);

            if (_decreaseButton != null)
                _decreaseButton.onClick.AddListener(OnDecreaseClicked);
        }

        /// <summary>
        /// 초기 반사 횟수를 표시한다.
        /// </summary>
        private void Start()
        {
            UpdateDisplay();
        }

        /// <summary>
        /// 버튼 클릭 이벤트를 해제한다.
        /// </summary>
        private void OnDisable()
        {
            if (_increaseButton != null)
                _increaseButton.onClick.RemoveListener(OnIncreaseClicked);

            if (_decreaseButton != null)
                _decreaseButton.onClick.RemoveListener(OnDecreaseClicked);
        }

        private void OnIncreaseClicked()
        {
            _playerController.IncreaseBounceCount();
            UpdateDisplay();
        }

        private void OnDecreaseClicked()
        {
            _playerController.DecreaseBounceCount();
            UpdateDisplay();
        }

        /// <summary>
        /// 텍스트에 현재 반사 횟수를 반영한다.
        /// </summary>
        private void UpdateDisplay()
        {
            if (_countText != null && _playerController != null)
                _countText.text = _playerController.BounceCount.ToString();
        }
    }
}
