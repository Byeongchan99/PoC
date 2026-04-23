using UnityEngine;

namespace POC3
{
    public class HexRing : MonoBehaviour
    {
        static readonly float[] GapWeights = { 0.20f, 0.30f, 0.30f, 0.10f, 0.05f, 0.05f };

        [SerializeField] GameObject[] sides = new GameObject[6];
        [SerializeField] float collisionScale = 1f;

        // t가 이 값을 넘으면 파괴 (t=2 → scale = 4 * collisionScale)
        const float DestroyT = 2f;

        float timer;
        float totalTime; // 링이 플레이어에 도달하기까지 걸리는 시간
        bool collisionChecked;

        void Awake()
        {
            transform.localScale = Vector3.zero;
            // 난이도에 따라 도달 시간 감소 (플레이어가 미리 볼 수 있는 시간이 줄어듦)
            totalTime = Mathf.Lerp(3f, 1f, GameManager.Instance.Difficulty);
            DisableGaps(RollGapCount());
        }

        static int RollGapCount()
        {
            float r = Random.value;
            float cumulative = 0f;
            for (int i = 0; i < GapWeights.Length; i++)
            {
                cumulative += GapWeights[i];
                if (r < cumulative) return i + 1;
            }
            return GapWeights.Length;
        }

        void DisableGaps(int count)
        {
            int[] indices = { 0, 1, 2, 3, 4, 5 };

            // Fisher-Yates 셔플로 랜덤 인덱스 선택
            for (int i = 5; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }

            for (int i = 0; i < count; i++)
                sides[indices[i]].SetActive(false);
        }

        void Update()
        {
            if (GameManager.Instance.CurrentState != GameManager.State.Playing) return;

            timer += Time.deltaTime;
            float t = timer / totalTime;

            // 이차 ease-in: 처음엔 느리게(갭 확인), 플레이어에 가까워질수록 빠르게
            transform.localScale = Vector3.one * (t * t * collisionScale);

            if (!collisionChecked && t >= 1f)
            {
                collisionChecked = true;
                CheckCollision();
            }

            if (t > DestroyT)
                Destroy(gameObject);
        }

        void CheckCollision()
        {
            // 플레이어는 고정 위치 → 씬 배치 기준 각도 사용
            // 링은 WorldContainer의 자식이므로 transform.eulerAngles.z에 WorldContainer 회전이 포함됨
            float playerAngle = PlayerController.Instance.PlayerAngle;
            float ringRot = transform.eulerAngles.z;

            float localAngle = ((playerAngle - ringRot) % 360f + 360f) % 360f;
            int sideIndex = Mathf.FloorToInt(localAngle / 60f) % 6;

            if (sides[sideIndex].activeSelf)
                GameManager.Instance.TriggerGameOver();
        }
    }
}
