using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 플레이어 사망 시 표시되는 게임 오버 화면.
/// Time.timeScale을 0으로 멈추고, 도달한 인카운터 수와 재시작 버튼을 표시합니다.
/// RunManager.OnPlayerDied()에서 Show()를 호출합니다.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    private const string RunSceneName = "CardTest";

    [Header("UI 참조")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI encounterCountText;
    [SerializeField] private Button          restartButton;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (restartButton != null) restartButton.onClick.AddListener(OnRestartClicked);
    }

    /// <summary>게임 오버 화면을 표시합니다. RunManager가 호출합니다.</summary>
    public void Show(int reachedEncounter)
    {
        if (panel != null) panel.SetActive(true);

        if (encounterCountText != null)
            encounterCountText.text = $"도달한 인카운터: {reachedEncounter}";

        Time.timeScale = 0f;
    }

    private void OnRestartClicked()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(RunSceneName);
    }
}
