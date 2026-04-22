using UnityEngine;

/// <summary>
/// 기관차에 꼬리 이펙트를 적용하는 컴포넌트.
/// TrailRenderer의 주요 설정을 Inspector에서 한 곳에서 조정 가능하도록 래핑.
/// </summary>
[RequireComponent(typeof(TrailRenderer))]
public class TrainTrailEffect : MonoBehaviour
{
    [Header("꼬리 이펙트 설정")]
    [Tooltip("꼬리가 화면에 남아있는 시간 (초). 클수록 꼬리가 길어짐")]
    [SerializeField] private float _time = 0.5f;

    [Tooltip("꼬리의 시작 부분(기관차 쪽) 두께")]
    [SerializeField] private float _startWidth = 0.5f;

    [Tooltip("꼬리의 끝 부분 두께. 0이면 끝이 뾰족하게 사라짐")]
    [SerializeField] private float _endWidth = 0f;

    [Tooltip("꼬리의 색상 그라디언트. 시작~끝 색상을 Inspector의 그라디언트 에디터로 지정")]
    [SerializeField] private Gradient _colorGradient;

    private TrailRenderer _trailRenderer;

    private void Awake()
    {
        _trailRenderer = GetComponent<TrailRenderer>();
        ApplySettings();
    }

    /// <summary>
    /// SerializeField로 설정한 값을 TrailRenderer에 적용.
    /// </summary>
    private void ApplySettings()
    {
        _trailRenderer.time = _time;
        _trailRenderer.startWidth = _startWidth;
        _trailRenderer.endWidth = _endWidth;
        _trailRenderer.colorGradient = _colorGradient;
    }
}
