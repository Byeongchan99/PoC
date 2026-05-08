using UnityEngine;

namespace POC7
{
    /// <summary>
    /// 게임 전체 흐름을 조율하는 컴포넌트.
    /// 각 시스템 참조를 Inspector에서 연결받아 게임 시작/종료를 관리한다.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private RingController _ringController;
        [SerializeField] private EnemySpawner _enemySpawner;
        [SerializeField] private ObstacleSpawner _obstacleSpawner;
        [SerializeField] private SaturationSystem _saturationSystem;

        /// <summary>
        /// 게임 시작 시 첫 웨이브를 강제 스폰하여 플레이어가 바로 행동할 상황을 만든다.
        /// 기획에 따라 비활성화할 수 있다.
        /// </summary>
        private void Start()
        {
            // 첫 클릭 전에 적이 없으면 게임이 밋밋하므로 시작 시 1웨이브를 미리 스폰한다.
            // 기획 변경 시 아래 줄을 주석 처리하면 된다.
            SpawnInitialWave();
        }

        /// <summary>
        /// 오브젝트 활성화 시 게임 오버 이벤트를 구독한다.
        /// </summary>
        private void OnEnable()
        {
            SaturationSystem.OnGameOver += HandleGameOver;
        }

        /// <summary>
        /// 오브젝트 비활성화 시 이벤트 구독을 해제한다.
        /// </summary>
        private void OnDisable()
        {
            SaturationSystem.OnGameOver -= HandleGameOver;
        }

        /// <summary>
        /// 게임 오버 이벤트 수신 시 호출된다. 시간을 정지하고 게임 오버 메시지를 출력한다.
        /// </summary>
        private void HandleGameOver()
        {
            Debug.Log("[GameManager] 게임 오버: 포화도 임계치 초과");
            Time.timeScale = 0f;
        }

        /// <summary>
        /// 게임 시작 시 첫 웨이브를 강제 스폰한다.
        /// EnemySpawner의 OnDashStarted 구독과 동일한 SpawnWave 로직을 외부에서 트리거한다.
        /// </summary>
        private void SpawnInitialWave()
        {
            if (_enemySpawner == null)
            {
                Debug.LogWarning("[GameManager] EnemySpawner가 연결되지 않았습니다.");
                return;
            }

            // EnemySpawner는 OnDashStarted 이벤트로 스폰하므로
            // 시작 시 동일 이벤트를 강제 발생시키는 대신 public 메서드로 노출한다.
            _enemySpawner.SpawnInitialWave();
            _obstacleSpawner?.SpawnInitialWave();
        }
    }
}
