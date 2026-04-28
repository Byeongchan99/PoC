using System.Collections;
using UnityEngine;
using POC5.Graph;

namespace POC5.Runtime
{
    /// <summary>
    /// 씬의 틱 시스템 매니저.
    /// NodeGraph를 보유하고 일정 간격(tickInterval)마다 Graph.Tick()을 호출한다.
    /// FacilityNode 등록/해제 창구 역할도 한다.
    ///
    /// 사용법: 씬에 빈 게임 오브젝트를 만들고 이 컴포넌트를 붙인다.
    ///
    /// 실무 팁: WaitForSeconds는 Start()에서 한 번만 생성해 재사용한다.
    ///          매 틱 new WaitForSeconds()를 호출하면 GC 압박이 생긴다.
    /// </summary>
    public class ResourceFlowSystem : MonoBehaviour
    {
        [Tooltip("틱 간격 (초). 낮을수록 자원이 빠르게 흐른다.")]
        [SerializeField] private float _tickInterval = 1f;

        /// <summary>이 시스템이 관리하는 노드 그래프.</summary>
        public NodeGraph Graph { get; private set; }

        private void Awake()
        {
            Graph = new NodeGraph();
        }

        private void Start()
        {
            StartCoroutine(TickLoop());
        }

        /// <summary>
        /// tickInterval마다 그래프의 Tick()을 호출하는 코루틴.
        /// </summary>
        private IEnumerator TickLoop()
        {
            // WaitForSeconds를 미리 생성해 GC 할당을 줄인다
            var wait = new WaitForSeconds(_tickInterval);
            while (true)
            {
                Graph.Tick();
                yield return wait;
            }
        }

        /// <summary>
        /// FacilityNode를 그래프에 등록한다.
        /// FacilityNode의 Initialize() 또는 Awake() 이후에 호출해야 GraphNode가 존재한다.
        /// </summary>
        public void RegisterFacility(FacilityNode facility)
        {
            if (facility?.GraphNode == null)
            {
                Debug.LogWarning($"[ResourceFlowSystem] 등록 실패: {facility?.name}의 GraphNode가 null입니다.");
                return;
            }
            Graph.AddNode(facility.GraphNode);
            Debug.Log($"[ResourceFlowSystem] {facility.name} 등록 완료");
        }

        /// <summary>
        /// FacilityNode를 그래프에서 제거한다.
        /// </summary>
        public void UnregisterFacility(FacilityNode facility)
        {
            if (facility?.GraphNode == null) return;
            Graph.RemoveNode(facility.GraphNode);
        }
    }
}
