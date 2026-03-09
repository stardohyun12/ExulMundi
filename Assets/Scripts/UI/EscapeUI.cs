using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 중 탈출 옵션을 제공하는 UI.
/// HP 소모 또는 카드 제거로 현재 인카운터를 건너뜁니다.
/// </summary>
public class EscapeUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button     escapeWithHPButton;
    [SerializeField] private Button     escapeWithCardButton;

    private void Awake() => panel?.SetActive(false);

    private void Start()
    {
        escapeWithHPButton?.onClick.AddListener(OnEscapeWithHP);
        escapeWithCardButton?.onClick.AddListener(OnEscapeWithCard);
    }

    public void Show() => panel?.SetActive(true);
    public void Hide() => panel?.SetActive(false);

    private void OnEscapeWithHP() => RunManager.Instance?.EscapeWithHP();

    private void OnEscapeWithCard()
    {
        var cards = CardInventory.Instance?.Cards;
        if (cards == null || cards.Count == 0) return;
        // 마지막으로 추가된 카드를 제거
        RunManager.Instance?.EscapeWithCard(cards[cards.Count - 1]);
    }
}
