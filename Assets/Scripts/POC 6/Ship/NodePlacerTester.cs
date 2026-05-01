using UnityEngine;

namespace POC6
{
    /// <summary>
    /// NodePlacer 동작 확인용 임시 테스트 스크립트입니다.
    /// Play 모드 시작 시 지정한 NodeData로 배치 모드를 자동으로 시작합니다.
    /// 테스트 완료 후 삭제해도 됩니다.
    /// </summary>
    public class NodePlacerTester : MonoBehaviour
    {
        [Tooltip("배치 테스트에 사용할 NodeData 에셋을 연결하세요.")]
        [SerializeField] private NodeData _testNode;

        [SerializeField] private NodePlacer _nodePlacer;

        private void Start()
        {
            if (_testNode == null)
            {
                Debug.LogWarning("[NodePlacerTester] TestNode가 연결되지 않았습니다.");
                return;
            }

            if (_nodePlacer == null)
            {
                Debug.LogWarning("[NodePlacerTester] NodePlacer가 연결되지 않았습니다.");
                return;
            }

            _nodePlacer.BeginPlacement(_testNode);
            Debug.Log($"[NodePlacerTester] '{_testNode.NodeName}' 배치 모드 시작. 클릭으로 배치, R키로 회전, ESC로 취소.");
        }
    }
}
