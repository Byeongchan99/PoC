using UnityEngine;
using UnityEngine.InputSystem;

namespace POC3
{
    [DefaultExecutionOrder(-1)]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        // HexRing.Awake에서 const로 참조하므로 Instance 초기화 순서 무관
        public const float OrbitRadius = 3f;

        int targetSector = 4;
        float currentAngle = (4 + 0.5f) * 60f; // 270° = 화면 하단

        public float CurrentAngleDeg => currentAngle;

        void Awake()
        {
            Instance = this;
            CreateTriangleMesh();
        }

        void CreateTriangleMesh()
        {
            float s = OrbitRadius * 0.08f;

            var mesh = new Mesh();
            mesh.vertices = new Vector3[]
            {
                new(0f,          s,           0f), // 팁 (중심 방향, +Y)
                new( s * 0.65f, -s * 0.6f,   0f), // 우하
                new(-s * 0.65f, -s * 0.6f,   0f), // 좌하
            };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.RecalculateNormals();

            gameObject.AddComponent<MeshFilter>().mesh = mesh;

            var mr = gameObject.AddComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                      ?? Shader.Find("Unlit/Color");
            mr.material = new Material(shader) { color = new Color(1f, 0.87f, 0f) };
        }

        void Update()
        {
            if (GameManager.Instance.CurrentState != GameManager.State.Playing) return;

            if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
                targetSector = (targetSector - 1 + 6) % 6;
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
                targetSector = (targetSector + 1) % 6;

            float targetAngle = (targetSector + 0.5f) * 60f;
            currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, 12f * Time.deltaTime);

            float rad = currentAngle * Mathf.Deg2Rad;
            transform.position = new Vector3(
                Mathf.Cos(rad) * OrbitRadius,
                Mathf.Sin(rad) * OrbitRadius,
                -0.5f); // 링 스프라이트 앞에 렌더링

            // currentAngle + 90° → 로컬 +Y(팁)가 항상 중심을 향함
            transform.rotation = Quaternion.Euler(0f, 0f, currentAngle + 90f);
        }
    }
}
