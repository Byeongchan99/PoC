using UnityEngine;
using UnityEngine.SceneManagement;

namespace POC3
{
    [DefaultExecutionOrder(-10)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum State { WaitingToStart, Playing, GameOver }
        public State CurrentState { get; private set; } = State.WaitingToStart;

        float survivalTime;

        // 0 → 1 over 60 seconds
        public float Difficulty => Mathf.Clamp01(survivalTime / 60f);

        void Awake() => Instance = this;

        void Update()
        {
            if (CurrentState == State.Playing)
                survivalTime += Time.deltaTime;

            if (CurrentState == State.WaitingToStart && Input.anyKeyDown)
                CurrentState = State.Playing;

            if (CurrentState == State.GameOver && Input.anyKeyDown)
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void TriggerGameOver()
        {
            if (CurrentState != State.Playing) return;
            CurrentState = State.GameOver;
        }

        void OnGUI()
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };

            switch (CurrentState)
            {
                case State.WaitingToStart:
                    style.normal.textColor = Color.white;
                    GUI.Label(FullRect(-30), "HEXAGON DODGE", style);
                    style.fontSize = 20;
                    GUI.Label(FullRect(20), "Press any key to start", style);
                    break;

                case State.Playing:
                    style.normal.textColor = Color.white;
                    GUI.Label(new Rect(0, 10, Screen.width, 50), $"{survivalTime:F2}s", style);
                    break;

                case State.GameOver:
                    style.normal.textColor = Color.red;
                    GUI.Label(FullRect(-60), "GAME OVER", style);
                    style.normal.textColor = Color.white;
                    style.fontSize = 22;
                    GUI.Label(FullRect(0), $"{survivalTime:F2}s survived", style);
                    style.fontSize = 16;
                    style.normal.textColor = new Color(1f, 1f, 1f, 0.6f);
                    GUI.Label(FullRect(45), "Press any key to restart", style);
                    break;
            }
        }

        static Rect FullRect(float yOffset) =>
            new(0, Screen.height / 2f + yOffset, Screen.width, 50);
    }
}
