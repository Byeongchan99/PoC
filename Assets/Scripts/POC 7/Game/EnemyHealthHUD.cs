using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace POC7
{
    /// <summary>
    /// 씬의 모든 활성 적 위에 체력 수치를 표시하는 World Space HUD.
    /// 이 컴포넌트를 부착한 GameObject에 Canvas 컴포넌트도 함께 존재해야 한다.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    public class EnemyHealthHUD : MonoBehaviour
    {
        /// <summary>체력 텍스트의 Y축 오프셋 (적 중심 기준 위쪽으로 띄우는 거리).</summary>
        [SerializeField] private float _labelYOffset = 0.4f;

        /// <summary>텍스트 폰트 크기. Canvas scale 0.01 기준 36 → 화면에서 약 0.36 유닛 크기.</summary>
        [SerializeField] private float _fontSize = 36f;

        [SerializeField] private Color _labelColor = Color.white;

        private Canvas _canvas;

        // 각 적과 해당 체력 텍스트를 매핑한다
        private readonly Dictionary<Enemy, TMP_Text> _labels = new();

        /// <summary>
        /// Canvas를 World Space 모드로 초기화한다.
        /// scale을 0.01로 설정하여 fontSize 36이 약 0.36 world unit 크기로 보이도록 조정한다.
        /// </summary>
        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;

            // 스프라이트보다 위에 렌더링되도록 sortingOrder를 높게 설정한다
            _canvas.sortingOrder = 100;

            // Canvas scale 0.01: fontSize 36 → 실제 크기 0.36 world unit
            transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        }

        /// <summary>
        /// 오브젝트 활성화 시 적 스폰/처치 이벤트를 구독한다.
        /// </summary>
        private void OnEnable()
        {
            Enemy.OnEnemySpawned += AddLabel;
            Enemy.OnEnemyKilled += RemoveLabel;
        }

        /// <summary>
        /// 오브젝트 비활성화 시 이벤트를 해제하고 남은 레이블을 모두 제거한다.
        /// </summary>
        private void OnDisable()
        {
            Enemy.OnEnemySpawned -= AddLabel;
            Enemy.OnEnemyKilled -= RemoveLabel;

            foreach (TMP_Text label in _labels.Values)
            {
                if (label != null)
                    Destroy(label.gameObject);
            }
            _labels.Clear();
        }

        /// <summary>
        /// 매 프레임 마지막에 레이블 위치를 해당 적의 world position에 맞춰 갱신한다.
        /// LateUpdate를 사용하여 적의 이동이 모두 반영된 후에 위치를 설정한다.
        /// </summary>
        private void LateUpdate()
        {
            foreach (KeyValuePair<Enemy, TMP_Text> pair in _labels)
            {
                if (pair.Key == null || pair.Value == null)
                    continue;

                pair.Value.transform.position =
                    pair.Key.transform.position + Vector3.up * _labelYOffset;
            }
        }

        /// <summary>
        /// 스폰된 적에 대한 체력 레이블을 생성하고, 체력 변경 이벤트를 구독한다.
        /// </summary>
        private void AddLabel(Enemy enemy)
        {
            GameObject go = new GameObject($"Label_{enemy.name}");
            go.transform.SetParent(transform, false);

            TMP_Text text = go.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = _fontSize;
            text.color = _labelColor;
            text.enableWordWrapping = false;
            text.text = enemy.CurrentHealth.ToString();

            // Canvas scale 0.01 기준: sizeDelta (200, 80) → world 크기 (2, 0.8)
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 80f);

            _labels[enemy] = text;

            // 체력이 변경될 때마다 텍스트를 갱신한다
            enemy.OnHealthChanged += health => UpdateLabel(enemy, health);
        }

        /// <summary>
        /// 처치된 적의 레이블을 제거한다.
        /// </summary>
        private void RemoveLabel(Enemy enemy)
        {
            if (!_labels.TryGetValue(enemy, out TMP_Text text))
                return;

            if (text != null)
                Destroy(text.gameObject);

            _labels.Remove(enemy);
        }

        /// <summary>
        /// 지정한 적의 체력 텍스트를 갱신한다.
        /// </summary>
        private void UpdateLabel(Enemy enemy, int health)
        {
            if (_labels.TryGetValue(enemy, out TMP_Text text) && text != null)
                text.text = health.ToString();
        }
    }
}
