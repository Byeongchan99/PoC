using UnityEngine;

namespace POC6
{
    /// <summary>
    /// 게임 전반에 걸쳐 사용되는 전역 설정값을 담는 ScriptableObject입니다.
    /// 밸런스 조정에 자주 쓰이는 수치들을 인스펙터에서 편집할 수 있습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "POC6/Data/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("시작 설정")]
        [Tooltip("게임 시작 시 지급되는 초기 골드")]
        [Min(0)]
        [SerializeField] private int _startingGold = 0;

        [Header("그리드 설정")]
        [Tooltip("그리드 셀 하나의 월드 좌표 크기 (유닛)")]
        [Min(0.1f)]
        [SerializeField] private float _cellSize = 1f;

        [Tooltip("우주선 그리드의 총 가로 셀 수")]
        [Range(3, 20)]
        [SerializeField] private int _gridWidth = 9;

        [Tooltip("우주선 그리드의 총 세로 셀 수")]
        [Range(3, 20)]
        [SerializeField] private int _gridHeight = 9;

        [Header("Build Phase 설정")]
        [Tooltip("빌드 단계에서 Time.timeScale 값. 0이면 완전 정지.")]
        [Range(0f, 1f)]
        [SerializeField] private float _buildPhaseTimeScale = 0f;

        [Header("적 스폰 설정")]
        [Tooltip("화면 밖 적 스폰 반경 (카메라 시야 반지름 + 여유값)")]
        [Min(1f)]
        [SerializeField] private float _enemySpawnRadius = 20f;

        [Header("노드 업그레이드 설정")]
        [Tooltip("노드 업그레이드 시 비용 증가 배수 (레벨당)")]
        [Range(1f, 3f)]
        [SerializeField] private float _upgradeCostMultiplier = 1.5f;

        [Tooltip("노드 업그레이드 시 스탯 증가 비율 (레벨당)")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _upgradeStatBonus = 0.2f;

        [Tooltip("노드 업그레이드 기본 비용 (1레벨 → 2레벨)")]
        [Min(1)]
        [SerializeField] private int _baseUpgradeCost = 30;

        [Header("카드 선택 설정")]
        [Tooltip("웨이브 클리어 후 제시되는 카드 수")]
        [Range(2, 5)]
        [SerializeField] private int _cardChoiceCount = 3;

        // 읽기 전용 프로퍼티들
        public int StartingGold => _startingGold;
        public float CellSize => _cellSize;
        public int GridWidth => _gridWidth;
        public int GridHeight => _gridHeight;
        public float BuildPhaseTimeScale => _buildPhaseTimeScale;
        public float EnemySpawnRadius => _enemySpawnRadius;
        public float UpgradeCostMultiplier => _upgradeCostMultiplier;
        public float UpgradeStatBonus => _upgradeStatBonus;
        public int BaseUpgradeCost => _baseUpgradeCost;
        public int CardChoiceCount => _cardChoiceCount;
    }
}
