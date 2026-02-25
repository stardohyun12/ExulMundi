using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 핸드(손패) 관리자.
/// 덱에서 카드를 드로우하고, 사용된 카드를 버림 더미로 보냄.
/// 핸드 크기 제한 없음.
/// </summary>
public class HandManager : MonoBehaviour
{
    public static HandManager Instance { get; private set; }

    [Header("핸드 설정")]
    public Transform   handArea;    // 핸드 카드 배치 부모 (HorizontalLayoutGroup 권장)
    public GameObject  cardPrefab;  // 카드 프리팹 (CardDisplay + CardClickHandler 포함)
    public Transform   dragLayer;   // 드래그 최상단 레이어 (현재 미사용, 확장용)

    [Header("초기 드로우")]
    public int initialDrawCount = 5; // 전투 시작 시 드로우 수

    // ──────────────────────────────────────
    // 내부 상태
    // ──────────────────────────────────────
    private List<CardData>   _deck        = new();
    private List<CardData>   _discardPile = new();
    private List<GameObject> _handCards   = new();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ══════════════════════════════════════
    // 공개 API
    // ══════════════════════════════════════

    /// <summary>전투 시작 시 덱 구성 + 초기 드로우</summary>
    public void SetupAndDraw(CardData[] cards)
    {
        ClearHand();
        _deck.Clear();
        _discardPile.Clear();

        foreach (var c in cards)
            _deck.Add(c);

        ShuffleDeck();
        DrawCards(initialDrawCount);
    }

    public void DrawCards(int count)
    {
        for (int i = 0; i < count; i++)
            DrawOne();
    }

    public void DrawOne()
    {
        if (_deck.Count == 0) RecycleDiscard();
        if (_deck.Count == 0) return;

        CardData card = _deck[0];
        _deck.RemoveAt(0);
        SpawnCardInHand(card);
    }

    /// <summary>카드 사용 후 핸드에서 제거 → 버림 더미</summary>
    public void OnCardUsed(GameObject cardGO, CardData cardData)
    {
        _handCards.Remove(cardGO);
        Destroy(cardGO);
        _discardPile.Add(cardData);
    }

    /// <summary>핸드 전체 버리기 (BurnHand 카드용) — 버린 카드 목록 반환</summary>
    public List<CardData> BurnAllHand()
    {
        var burned = new List<CardData>();
        foreach (var go in _handCards)
        {
            var handler = go.GetComponent<CardClickHandler>();
            if (handler != null) burned.Add(handler.CardData);
            Destroy(go);
        }
        _handCards.Clear();
        _discardPile.AddRange(burned);
        return burned;
    }

    /// <summary>패 교체 — 핸드 전체 버리고 같은 수 드로우</summary>
    public void RefreshHand()
    {
        int count = _handCards.Count;
        BurnAllHand();
        DrawCards(count);
    }

    // ══════════════════════════════════════
    // 내부 메서드
    // ══════════════════════════════════════

    private void SpawnCardInHand(CardData cardData)
    {
        if (cardPrefab == null || handArea == null) return;

        GameObject go = Instantiate(cardPrefab, handArea);
        _handCards.Add(go);

        var display = go.GetComponent<CardDisplay>();
        if (display != null) display.SetupCard(cardData);

        var handler = go.GetComponent<CardClickHandler>();
        if (handler != null) handler.Initialize(cardData);
    }

    private void ClearHand()
    {
        foreach (var go in _handCards)
            if (go != null) Destroy(go);
        _handCards.Clear();
    }

    private void RecycleDiscard()
    {
        _deck.AddRange(_discardPile);
        _discardPile.Clear();
        ShuffleDeck();
    }

    private void ShuffleDeck()
    {
        for (int i = _deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (_deck[i], _deck[j]) = (_deck[j], _deck[i]);
        }
    }
}
