using UnityEngine;

namespace POC6
{
    /// <summary>
    /// 노드 한 종류의 기본 설정 데이터를 담는 ScriptableObject입니다.
    /// 인스펙터에서 직접 편집해서 코어, 특수, 공격, 일반 노드를 정의합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Node_New", menuName = "POC6/Data/NodeData")]
    public class NodeData : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("노드 이름 (예: 코어, 레이저 포탑 등)")]
        [SerializeField] private string _nodeName;

        [Tooltip("노드의 종류 (Core / Special / Attack / Normal)")]
        [SerializeField] private NodeType _nodeType;

        [Tooltip("그리드에서 차지하는 크기 (예: (1,1), (2,2))")]
        [SerializeField] private Vector2Int _size = Vector2Int.one;

        [Tooltip("체력에 기여하는 수치. 모든 노드의 합이 우주선 최대 체력이 됩니다.")]
        [SerializeField] private int _healthContribution = 10;

        [Header("비주얼")]
        [Tooltip("씬에 배치될 노드의 게임오브젝트 프리팹")]
        [SerializeField] private GameObject _visualPrefab;

        [Tooltip("카드 UI 등에 사용할 아이콘 스프라이트")]
        [SerializeField] private Sprite _icon;

        [Tooltip("노드 색상 (프리미티브 도형에 적용)")]
        [SerializeField] private Color _tintColor = Color.white;

        [Header("코어 전용 설정")]
        [Tooltip("코어가 공급하는 총 동력량. 연결된 공격 노드들에게 균등하게 분배됩니다.")]
        [SerializeField] private int _powerCapacity = 100;

        [Header("공격 노드 전용 설정")]
        [Tooltip("발사구가 있는 면 방향. 이 면의 180도 전방으로 발사합니다.")]
        [SerializeField] private FaceDirection _attackFace = FaceDirection.Top;

        [Tooltip("공격 노드의 기본 전투 스탯")]
        [SerializeField] private AttackStats _baseAttackStats;

        [Header("특수 노드 전용 설정")]
        [Tooltip("이 특수 노드가 연결된 공격 노드에 부여하는 효과 종류")]
        [SerializeField] private SpecialEffectType _specialEffect;

        [Tooltip("특수 효과의 강도 (Multishot: 추가 발사체 수, Pierce: 추가 관통 수)")]
        [Range(1f, 10f)]
        [SerializeField] private float _effectMagnitude = 1f;

        // 읽기 전용 프로퍼티들
        public string NodeName => _nodeName;
        public NodeType NodeType => _nodeType;
        public Vector2Int Size => _size;
        public int HealthContribution => _healthContribution;
        public GameObject VisualPrefab => _visualPrefab;
        public Sprite Icon => _icon;
        public Color TintColor => _tintColor;
        public int PowerCapacity => _powerCapacity;
        public FaceDirection AttackFace => _attackFace;
        public AttackStats BaseAttackStats => _baseAttackStats;
        public SpecialEffectType SpecialEffect => _specialEffect;
        public float EffectMagnitude => _effectMagnitude;
    }
}
