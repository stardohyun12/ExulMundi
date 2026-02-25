using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 전투 중 핸드 카드에 붙는 클릭 핸들러.
/// 클릭 시 에너지 소모 → 카드 효과 적용 → 핸드에서 제거.
/// </summary>
[RequireComponent(typeof(CardDisplay))]
public class CardClickHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private CardData _cardData;

    // 에너지 부족 시 시각 피드백용
    private Image _cardImage;
    private Color _originalColor;

    public CardData CardData => _cardData;

    public void Initialize(CardData data)
    {
        _cardData = data;
        _cardImage = GetComponent<Image>();
        if (_cardImage != null) _originalColor = _cardImage.color;
    }

    void Update()
    {
        // 에너지 시스템 제거됨 - 항상 사용 가능
        if (_cardImage == null || _cardData == null) return;
        _cardImage.color = _originalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TryUseCard();
    }

    private void TryUseCard()
    {
        if (_cardData == null) return;
        if (BattleManager.Instance == null || !BattleManager.Instance.IsBattleActive) return;

        // 카드 효과 적용
        CardEffectApplier.Apply(
            _cardData,
            BattleManager.Instance.HeroUnit,
            BattleManager.Instance.CurrentEnemy);

        // 핸드에서 제거
        HandManager.Instance?.OnCardUsed(gameObject, _cardData);
    }

    // 에너지 부족 시 흔들기 피드백
    private System.Collections.IEnumerator ShakeFeedback()
    {
        Vector3 origin = transform.localPosition;
        float   elapsed = 0f;
        float   duration = 0.3f;
        float   strength = 8f;

        while (elapsed < duration)
        {
            float x = Mathf.Sin(elapsed * 40f) * strength * (1f - elapsed / duration);
            transform.localPosition = origin + new Vector3(x, 0, 0);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localPosition = origin;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // CardHover가 있으면 위임, 없으면 자체 확대
        if (GetComponent<CardHover>() == null)
            transform.localScale = Vector3.one * 1.1f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (GetComponent<CardHover>() == null)
            transform.localScale = Vector3.one;
    }
}
