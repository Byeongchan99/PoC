using UnityEngine;

/// <summary>
/// 기관차에 두 줄 레일 트레일 이펙트를 적용하는 컴포넌트.
/// 기관차 좌우에 자식 오브젝트를 생성하고 각각 TrailRenderer를 붙여
/// 실제 기차 레일처럼 두 줄의 흔적을 남김.
/// </summary>
public class TrainTrailEffect : MonoBehaviour
{
    [Header("레일 간격")]
    [Tooltip("왼쪽과 오른쪽 레일 사이의 거리. 기관차 너비에 맞게 조정")]
    [SerializeField] private float _trackSpacing = 0.3f;

    [Header("TrailRenderer 설정")]
    [Tooltip("레일 흔적이 화면에 남아있는 시간 (초). 클수록 긴 흔적이 남음")]
    [SerializeField] private float _time = 1f;

    [Tooltip("레일 선의 두께. 실제 레일처럼 얇게 설정 권장")]
    [SerializeField] private float _railWidth = 0.08f;

    [Tooltip("레일 색상 그라디언트")]
    [SerializeField] private Gradient _colorGradient;

    [Tooltip("TrailRenderer에 적용할 Material. 없으면 기본 Material 사용")]
    [SerializeField] private Material _railMaterial;

    private void Awake()
    {
        CreateRailTrail("LeftRail", -_trackSpacing * 0.5f);
        CreateRailTrail("RightRail", _trackSpacing * 0.5f);
    }

    /// <summary>
    /// 지정한 로컬 X 오프셋 위치에 레일 트레일 자식 오브젝트를 생성.
    /// 자식이 부모(기관차)의 회전을 따르므로 곡선 구간에서도 자연스럽게 휨.
    /// </summary>
    private void CreateRailTrail(string objName, float localOffsetX)
    {
        GameObject railObj = new GameObject(objName);
        railObj.transform.SetParent(transform);
        railObj.transform.localPosition = new Vector3(localOffsetX, 0f, 0f);
        railObj.transform.localRotation = Quaternion.identity;
        railObj.transform.localScale = Vector3.one;

        TrailRenderer trail = railObj.AddComponent<TrailRenderer>();
        trail.time = _time;
        trail.startWidth = _railWidth;
        trail.endWidth = _railWidth;
        trail.colorGradient = _colorGradient;

        if (_railMaterial != null)
            trail.material = _railMaterial;
    }
}
