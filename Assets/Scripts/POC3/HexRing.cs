using UnityEngine;

namespace POC3
{
    public class HexRing : MonoBehaviour
    {
        static readonly float[] GapWeights = { 0.20f, 0.30f, 0.30f, 0.10f, 0.05f, 0.05f };
        const float DestroyScale = 4f;

        // 프리팹에 미리 배치된 6개 변 오브젝트 (인스펙터에서 할당)
        [SerializeField] GameObject[] sides = new GameObject[6];

        float rotateSpeed;
        bool collisionChecked;

        void Awake()
        {
            transform.localScale = Vector3.zero;
            DisableGaps(RollGapCount());
            rotateSpeed = Random.Range(-20f, 20f);
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

            float expandSpeed = Mathf.Lerp(1.0f, 4.0f, GameManager.Instance.Difficulty);
            float s = transform.localScale.x + expandSpeed * Time.deltaTime;
            transform.localScale = Vector3.one * s;
            transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);

            // scale = 1 → 링이 플레이어 궤도에 도달
            if (!collisionChecked && s >= 1f)
            {
                collisionChecked = true;
                CheckCollision();
            }

            if (s > DestroyScale)
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
