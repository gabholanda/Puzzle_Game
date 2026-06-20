using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public string gameplaySceneName = "GameplayScene";

    void Start()
    {
        UIHelpers.EnsureEventSystem();
        var canvas = UIHelpers.CreateCanvas();

        UIHelpers.CreateStretch(canvas.transform, "Background", new Color(0.10f, 0.12f, 0.18f));

        UIHelpers.CreateText(canvas.transform, "Title",
            "Sliding Puzzle", 110, Color.white,
            new Vector2(960, 240), new Vector2(0, 350));

        UIHelpers.CreateText(canvas.transform, "Subtitle",
            "Slide the tiles into order 1-8.\nYou have a limited amount of moves.",
            42, new Color(0.75f, 0.80f, 0.88f),
            new Vector2(900, 200), new Vector2(0, 100));

        var startBtn = UIHelpers.CreateButton(canvas.transform, "StartButton",
            "Start Game", new Color(0.20f, 0.60f, 0.85f),
            new Vector2(520, 160), new Vector2(0, -200));
        startBtn.onClick.AddListener(() => SceneManager.LoadScene(gameplaySceneName));

        var quitBtn = UIHelpers.CreateButton(canvas.transform, "QuitButton",
            "Quit", new Color(0.35f, 0.38f, 0.45f),
            new Vector2(320, 110), new Vector2(0, -400));
        quitBtn.onClick.AddListener(() =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
    }
}
