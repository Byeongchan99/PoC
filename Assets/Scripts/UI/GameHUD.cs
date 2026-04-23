using TMPro;
using UnityEngine;

namespace POC2
{

/// <summary>
/// 화면 상단에 고정되는 HUD.
/// 현재 화물 / 최대 화물과 기차 칸 수를 매 프레임 갱신하여 표시.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private TrainManager _trainManager;

    [Header("UI 텍스트")]
    [SerializeField] private TextMeshProUGUI _cargoText;
    [SerializeField] private TextMeshProUGUI _carCountText;

    private void Update()
    {
        UpdateCargoText();
        UpdateCarCountText();
    }

    /// <summary>
    /// 현재 화물 / 최대 화물을 텍스트로 갱신.
    /// </summary>
    private void UpdateCargoText()
    {
        _cargoText.text = $"화물: {_trainManager.CurrentCargo} / {_trainManager.MaxCargo}";
    }

    /// <summary>
    /// 현재 기차 칸 수를 텍스트로 갱신.
    /// </summary>
    private void UpdateCarCountText()
    {
        _carCountText.text = $"기차 칸: {_trainManager.CarCount}";
    }
}
}
