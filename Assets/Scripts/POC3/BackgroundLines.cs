using UnityEngine;

namespace POC3
{
    public class BackgroundLines : MonoBehaviour
    {
        void Awake()
        {
            float len = 12f;
            var mat = new Material(Shader.Find("Sprites/Default"))
            {
                color = new Color(1f, 1f, 1f, 0.18f),
            };

            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                var go = new GameObject($"Line{i}");
                go.transform.SetParent(transform);

                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = true;
                lr.positionCount = 2;
                lr.SetPosition(0, Vector3.zero);
                lr.SetPosition(1, new Vector3(Mathf.Cos(angle) * len, Mathf.Sin(angle) * len, 0f));
                lr.startWidth = lr.endWidth = 0.03f;
                lr.material = mat;
                lr.sortingOrder = -1;
            }
        }
    }
}
