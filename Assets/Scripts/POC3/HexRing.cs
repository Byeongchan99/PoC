using UnityEngine;

namespace POC3
{
    public class HexRing : MonoBehaviour
    {
        // 갭 개수 확률 (인덱스 = 갭 수 - 1)
        static readonly float[] GapWeights = { 0.20f, 0.30f, 0.30f, 0.10f, 0.05f, 0.05f };

        // 정육각형 기하: 내접원 반지름 = 외접원 반지름 * √3/2
        const float Apothem = 0.866025f;
        const float ThicknessRatio = 0.05f;
        const float DestroyScale = 4f;

        readonly GameObject[] sides = new GameObject[6];
        float rotateSpeed;
        bool collisionChecked;

        // 모든 링이 동일한 Sprite 공유 (Texture 중복 생성 방지)
        static Sprite sharedSprite;

        void Awake()
        {
            float r = PlayerController.OrbitRadius;
            float thickness = r * ThicknessRatio;

            for (int i = 0; i < 6; i++)
            {
                // 변 i의 중심 각도: i*60° + 30°
                float midDeg = i * 60f + 30f;
                float midRad = midDeg * Mathf.Deg2Rad;

                var go = new GameObject($"Side{i}");
                go.transform.SetParent(transform, false);

                // 내접원 반지름 방향에 배치
                go.transform.localPosition = new Vector3(
                    Mathf.Cos(midRad) * Apothem * r,
                    Mathf.Sin(midRad) * Apothem * r,
                    0f);

                // 변 방향 = 중심 방향 + 90° (접선 방향)
                go.transform.localRotation = Quaternion.Euler(0f, 0f, midDeg + 90f);

                // 가로 = 변 길이(= 외접원 반지름), 세로 = 두께
                go.transform.localScale = new Vector3(r, thickness, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = GetSharedSprite();
                sr.color = Color.red;

                sides[i] = go;
            }

            DisableGaps(RollGapCount());
            rotateSpeed = Random.Range(-20f, 20f);
        }

        static Sprite GetSharedSprite()
        {
            if (sharedSprite != null) return sharedSprite;

            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color[16];
            for (int i = 0; i < 16; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();

            // PPU = 4: 4x4 텍스처 → 1x1 유닛 스프라이트
            sharedSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4f);
            return sharedSprite;
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

            // scale = 1 → 링의 외접원 반지름 = OrbitRadius = 플레이어 위치
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
            float playerAngle = PlayerController.Instance.CurrentAngleDeg;
            float ringRot = transform.eulerAngles.z;

            // 링 로컬 기준으로 플레이어 각도 변환
            float localAngle = ((playerAngle - ringRot) % 360f + 360f) % 360f;
            int sideIndex = Mathf.FloorToInt(localAngle / 60f) % 6;

            if (sides[sideIndex].activeSelf)
                GameManager.Instance.TriggerGameOver();
        }
    }
}
