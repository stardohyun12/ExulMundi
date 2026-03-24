using UnityEngine;
using System;

/// <summary>
/// 휴식 공간 진입·종료를 담당합니다.
/// RunManager가 3번 전투마다 Enter()를 호출합니다.
/// </summary>
public class RestRoomManager : MonoBehaviour
{
    public static RestRoomManager Instance { get; private set; }

    [SerializeField] private RestRoomUI restRoomUI;

    private Action _onComplete;

    /// <summary>휴식 공간 종료 시 발생합니다.</summary>
    public event Action OnRestRoomEnded;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>휴식 공간에 진입합니다. 완료 시 onComplete 콜백이 호출됩니다.</summary>
    public void Enter(Action onComplete)
    {
        _onComplete = onComplete;

        if (restRoomUI == null)
        {
            // UI가 연결되지 않은 경우 timeScale을 멈추지 않고 즉시 통과
            Debug.LogWarning("[RestRoomManager] restRoomUI가 없습니다. 휴식 공간을 건너뜁니다.");
            Complete();
            return;
        }

        Time.timeScale = 0f;
        restRoomUI.Show(this);
        Debug.Log("[RestRoomManager] 휴식 공간 진입");
    }

    /// <summary>HP +1 회복 선택지를 처리합니다.</summary>
    public void OnHealChosen()
    {
        var ph = FindFirstObjectByType<PlayerHealth>();
        ph?.Heal(1);
        Debug.Log("[RestRoomManager] 회복 선택 — HP+1");
        Complete();
    }

    /// <summary>두 카드를 합성해 새 카드로 교체합니다.</summary>
    public void OnSynthesizeChosen(CardData cardA, CardData cardB, CardData result)
    {
        if (cardA != null) CardInventory.Instance?.RemoveCard(cardA);
        if (cardB != null) CardInventory.Instance?.RemoveCard(cardB);
        if (result != null) CardInventory.Instance?.AddCard(result);
        Debug.Log($"[RestRoomManager] 합성 — {cardA?.cardName} + {cardB?.cardName} → {result?.cardName}");
        Complete();
    }

    /// <summary>CardRewardUI를 통해 카드 1장을 새로 선택합니다.</summary>
    public void OnReplaceChosen(CardData[] pool, Action<CardData> onCardChosen)
    {
        Debug.Log("[RestRoomManager] 교체 선택");
        onCardChosen += _ => Complete();
        FindFirstObjectByType<CardRewardUI>()?.Show(pool, onCardChosen);
    }

    /// <summary>아무 선택 없이 휴식 공간을 건너뜁니다.</summary>
    public void Skip()
    {
        Debug.Log("[RestRoomManager] 건너뛰기");
        Complete();
    }

    private void Complete()
    {
        restRoomUI?.Hide();
        Time.timeScale = 1f;
        OnRestRoomEnded?.Invoke();
        _onComplete?.Invoke();
        _onComplete = null;
        Debug.Log("[RestRoomManager] 휴식 공간 종료");
    }
}
