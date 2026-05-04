using System.Collections.Generic;
using UnityEngine;

namespace POC6
{
    /// <summary>
    /// 게임 시작 시 기본 우주선 형태를 그리드에 자동 배치합니다.
    /// 인스펙터에서 시작 배치 목록을 직접 정의할 수 있습니다.
    /// </summary>
    public class DefaultShipSetup : MonoBehaviour
    {
        [System.Serializable]
        public class StartNodeEntry
        {
            [Tooltip("배치할 노드 데이터")]
            public NodeData nodeData;

            [Tooltip("그리드 배치 위치 (좌상단 기준)")]
            public Vector2Int gridPosition;

            [Tooltip("회전 단계 (0=0도, 1=90도, 2=180도, 3=270도)")]
            [Range(0, 3)]
            public int rotationStep = 0;
        }

        [Header("기본 배치 설정")]
        [Tooltip("게임 시작 시 자동으로 배치할 노드 목록")]
        [SerializeField] private List<StartNodeEntry> _startNodes = new();

        [Header("참조")]
        [SerializeField] private ShipGrid _shipGrid;

        [Header("테스트")]
        [Tooltip("체크하면 Start()에서 자동으로 기본 배치를 실행합니다. GameManager 없이 단독 테스트용.")]
        [SerializeField] private bool _setupOnStart = false;

        private void Start()
        {
            if (_setupOnStart)
                SetupDefaultShip();
        }

        /// <summary>
        /// 정의된 기본 노드 목록을 그리드에 배치합니다.
        /// GameManager의 Init 단계에서 호출됩니다.
        /// </summary>
        public void SetupDefaultShip()
        {
            foreach (var entry in _startNodes)
            {
                if (entry.nodeData == null) continue;

                if (!_shipGrid.CanPlace(entry.nodeData, entry.gridPosition, entry.rotationStep))
                {
                    Debug.LogWarning($"[DefaultShipSetup] {entry.nodeData.NodeName}을(를) {entry.gridPosition}에 배치할 수 없습니다.");
                    continue;
                }

                var placedNode = new PlacedNode(entry.nodeData, entry.gridPosition, entry.rotationStep);
                GameObject worldObj = SpawnNodeVisual(placedNode);
                placedNode.WorldInstance = worldObj;
                _shipGrid.PlaceNode(placedNode);
            }
        }

        /// <summary>
        /// 스냅샷 데이터로부터 노드를 복원합니다.
        /// 웨이브 실패 후 이전 상태로 돌아올 때 사용합니다.
        /// </summary>
        public void RestoreFromSnapshot(List<PlacedNodeData> nodeDataList)
        {
            _shipGrid.Clear();

            foreach (var data in nodeDataList)
            {
                // 에셋 이름으로 NodeData ScriptableObject 로드
                NodeData nodeData = Resources.Load<NodeData>($"Assets/SO/POC 6/{data.nodeDataName}");

                if (nodeData == null)
                {
                    Debug.LogWarning($"[DefaultShipSetup] NodeData '{data.nodeDataName}'을(를) Resources에서 찾을 수 없습니다.");
                    continue;
                }

                var gridPos = new Vector2Int(data.gridX, data.gridY);
                var placedNode = new PlacedNode(nodeData, gridPos, data.rotationStep);
                placedNode.SetUpgradeLevel(data.upgradeLevel);

                GameObject worldObj = SpawnNodeVisual(placedNode);
                placedNode.WorldInstance = worldObj;
                _shipGrid.PlaceNode(placedNode);
            }
        }

        /// <summary>
        /// PlacedNode에 맞는 2D 씬 오브젝트를 생성하고 반환합니다.
        /// </summary>
        private GameObject SpawnNodeVisual(PlacedNode node)
        {
            return NodeVisualFactory.CreateNodeVisual(node, _shipGrid, _shipGrid.transform);
        }
    }
}
