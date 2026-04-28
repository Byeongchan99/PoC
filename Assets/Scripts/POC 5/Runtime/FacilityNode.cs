using UnityEngine;
using POC5.Data;
using POC5.Graph;

namespace POC5.Runtime
{
    /// <summary>
    /// 설비 노드의 Unity 컴포넌트.
    /// 씬에 배치되어 FacilityGraphNode(순수 C# 로직)를 소유하고 생명주기를 관리한다.
    ///
    /// 사용법 A (인스펙터): FacilityData를 Inspector에서 지정 → Awake에서 자동 초기화.
    /// 사용법 B (코드): AddComponent 직후 Initialize(data)를 호출해 초기화.
    /// </summary>
    public class FacilityNode : MonoBehaviour
    {
        [Tooltip("이 설비의 종류와 스탯을 담은 ScriptableObject.")]
        [SerializeField] private FacilityData _data;

        /// <summary>이 컴포넌트가 관리하는 순수 C# 그래프 노드.</summary>
        public FacilityGraphNode GraphNode { get; private set; }

        private void Awake()
        {
            if (_data != null)
                CreateGraphNode(_data);
        }

        /// <summary>
        /// 코드에서 직접 FacilityData를 지정해 초기화한다.
        /// AddComponent() 직후, Awake() 이전에 호출해야 한다.
        /// </summary>
        public void Initialize(FacilityData data)
        {
            _data = data;
            CreateGraphNode(data);
        }

        private void CreateGraphNode(FacilityData data)
        {
            GraphNode = new FacilityGraphNode(data);
        }
    }
}
