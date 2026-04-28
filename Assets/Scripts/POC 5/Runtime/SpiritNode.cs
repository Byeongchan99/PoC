using UnityEngine;
using POC5.Data;

namespace POC5.Runtime
{
    /// <summary>
    /// 스피릿 노드의 Unity 컴포넌트.
    /// SpiritData를 보유하며, 설비 노드에 드래그 앤 드롭으로 배치되는 단위.
    ///
    /// POC에서 체력/포만감 감소 로직은 없으며 데이터 표시 전용이다.
    /// 5단계에서 드래그 앤 드롭 UI와 속성 매칭 검증이 추가된다.
    /// </summary>
    public class SpiritNode : MonoBehaviour
    {
        [Tooltip("이 스피릿의 종족 메타데이터 ScriptableObject.")]
        [SerializeField] private SpiritData _data;

        // 현재 배치된 설비 (없으면 null)
        private FacilityNode _assignedFacility;

        /// <summary>이 스피릿의 종족 메타데이터.</summary>
        public SpiritData Data => _data;

        /// <summary>현재 배치된 설비 노드 (없으면 null).</summary>
        public FacilityNode AssignedFacility => _assignedFacility;

        /// <summary>
        /// 이 스피릿을 설비 노드에 배치한다.
        /// 이미 다른 설비에 배치 중이면 자동으로 해제 후 새 설비에 배치한다.
        /// </summary>
        public void AssignTo(FacilityNode facility)
        {
            if (facility?.GraphNode == null)
            {
                Debug.LogWarning($"[SpiritNode] 배치 실패: {facility?.name}의 GraphNode가 초기화되지 않았습니다.");
                return;
            }

            // 이전 설비에서 해제
            if (_assignedFacility != null)
                _assignedFacility.GraphNode.UnassignSpirit();

            _assignedFacility = facility;
            facility.GraphNode.AssignSpirit(_data);
            Debug.Log($"[SpiritNode] {_data.DisplayName}({_data.Element}) → {facility.name} 배치 완료");
        }

        /// <summary>
        /// 현재 배치된 설비에서 스피릿을 해제한다.
        /// </summary>
        public void Unassign()
        {
            if (_assignedFacility == null) return;
            _assignedFacility.GraphNode.UnassignSpirit();
            _assignedFacility = null;
        }
    }
}
