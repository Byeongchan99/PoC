using UnityEngine;

namespace POC6
{
    /// <summary>
    /// 특수 노드임을 나타내는 컴포넌트입니다.
    /// NodeVisualFactory가 Special 타입 노드 생성 시 자동으로 부착합니다.
    /// 특수 효과 실제 계산은 PowerGraph.GetEffectiveStats()에서 처리하며,
    /// 이 컴포넌트는 마킹 역할과 추후 시각적 피드백 확장을 위한 기반입니다.
    /// </summary>
    public class SpecialNodeBehaviour : MonoBehaviour
    {
        private PlacedNode _placedNode;

        /// <summary>
        /// 노드 생성 시 NodeVisualFactory에서 호출합니다.
        /// </summary>
        public void Initialize(PlacedNode node)
        {
            _placedNode = node;
        }
    }
}
