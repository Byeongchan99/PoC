using UnityEngine;

namespace POC3
{
    public class BackgroundCircle : MonoBehaviour
    {
        LineRenderer lr;
        float expandSpeed;
        float maxRadius;
        float baseAlpha;
        int segments;
        float radius;

        public void Init(float speed, float max, float width, Color color, int segs, Material mat, float startRadius = 0f)
        {
            expandSpeed = speed;
            maxRadius   = max;
            baseAlpha   = color.a;
            segments    = segs;
            radius      = startRadius;

            lr = gameObject.AddComponent<LineRenderer>();
            lr.loop          = true;
            lr.positionCount = segs;
            lr.startWidth    = lr.endWidth = width;
            lr.useWorldSpace = true;
            lr.material      = mat != null ? mat : new Material(Shader.Find("Sprites/Default"));
            lr.startColor    = lr.endColor = color;

            UpdatePositions();
        }

        void Update()
        {
            radius += expandSpeed * Time.deltaTime;

            // 바깥으로 갈수록 선이 흐려지는 효과
            float alpha = baseAlpha * (1f - radius / maxRadius);
            var c = new Color(lr.startColor.r, lr.startColor.g, lr.startColor.b, Mathf.Max(0f, alpha));
            lr.startColor = lr.endColor = c;

            UpdatePositions();

            if (radius >= maxRadius)
                Destroy(gameObject);
        }

        void UpdatePositions()
        {
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
            }
        }
    }
}
