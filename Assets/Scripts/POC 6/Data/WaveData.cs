using System;
using UnityEngine;

namespace POC6
{
    /// <summary>
    /// 단일 웨이브에서 등장할 적의 종류, 수량, 등장 간격을 정의하는 데이터입니다.
    /// WaveData의 SpawnInfos 배열에 여러 개 등록해서 웨이브를 구성합니다.
    /// </summary>
    [Serializable]
    public class WaveSpawnInfo
    {
        [Tooltip("등장할 적 종류")]
        [SerializeField] private EnemyData _enemyType;

        [Tooltip("이 그룹에서 스폰할 총 적 수")]
        [Min(1)]
        [SerializeField] private int _count = 5;

        [Tooltip("웨이브 시작 후 이 그룹이 등장하기까지 대기 시간 (초)")]
        [Min(0f)]
        [SerializeField] private float _spawnDelay = 0f;

        [Tooltip("이 그룹 내에서 적 한 마리씩 스폰되는 간격 (초)")]
        [Min(0.1f)]
        [SerializeField] private float _spawnInterval = 0.5f;

        // 읽기 전용 프로퍼티들
        public EnemyData EnemyType => _enemyType;
        public int Count => _count;
        public float SpawnDelay => _spawnDelay;
        public float SpawnInterval => _spawnInterval;
    }

    /// <summary>
    /// 웨이브 한 개의 전체 설정 데이터를 담는 ScriptableObject입니다.
    /// 인스펙터에서 5개 웨이브를 직접 편집할 수 있습니다.
    /// </summary>
    [CreateAssetMenu(fileName = "Wave_01", menuName = "POC6/Data/WaveData")]
    public class WaveData : ScriptableObject
    {
        [Header("웨이브 기본 정보")]
        [Tooltip("웨이브 번호 (1~5)")]
        [Range(1, 10)]
        [SerializeField] private int _waveNumber = 1;

        [Tooltip("보스 웨이브 여부. 마지막 웨이브에 체크합니다.")]
        [SerializeField] private bool _isBossWave = false;

        [Header("스폰 설정")]
        [Tooltip("이 웨이브에서 등장할 적 그룹 목록. 여러 그룹을 추가해서 다양한 구성을 만들 수 있습니다.")]
        [SerializeField] private WaveSpawnInfo[] _spawnInfos;

        // 읽기 전용 프로퍼티들
        public int WaveNumber => _waveNumber;
        public bool IsBossWave => _isBossWave;
        public WaveSpawnInfo[] SpawnInfos => _spawnInfos;

        /// <summary>
        /// 이 웨이브에서 스폰되는 총 적 수를 계산해서 반환합니다.
        /// </summary>
        public int GetTotalEnemyCount()
        {
            int total = 0;
            if (_spawnInfos == null) return total;

            foreach (var info in _spawnInfos)
            {
                total += info.Count;
            }

            return total;
        }
    }
}
