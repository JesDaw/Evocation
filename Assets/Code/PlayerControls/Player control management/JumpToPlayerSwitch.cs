using UnityEngine;
using DG.Tweening;
using UnityEngine.EventSystems;
using System.Collections.Generic;
public class JumpToPlayerSwitch : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] GameObject playerRootObject;
    [SerializeField] GameObject[] playerModles;
    [SerializeField] Vector3 highlightSize = new Vector3(1.5f, 1.5f, 1.5f );
    [SerializeField] float highlightTransitionTime = .3f;
    List<SpriteRenderer> playerParts = new List<SpriteRenderer>();
    int[] originalSortingOrders;

    void Start()
    {
        foreach (var model in playerModles)
        {
            if (model == null) continue;
            playerParts.AddRange(model.GetComponentsInChildren<SpriteRenderer>(true));
        }

        originalSortingOrders = new int[playerParts.Count];
        for (int i = 0; i < playerParts.Count; i++)
        {
            originalSortingOrders[i] = playerParts[i].sortingOrder;
        }
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        HighlightPleyer(true);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        HighlightPleyer(false);
    }

    void HighlightPleyer(bool highlight)
    {
        int orderBoost = highlight ? 100 : 0;
        for (int i = 0; i < playerParts.Count; i++)
        {
            playerParts[i].sortingOrder = originalSortingOrders[i] + orderBoost;
        }

        Vector3 targetSize = highlight ? highlightSize : new Vector3(1f, 1f, 1f);
        foreach (var player in playerModles) player.transform.DOScale(targetSize, highlightTransitionTime);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        HighlightPleyer(false);
        PlayerSwitch.Instance.SwitchToPlayer(playerRootObject);

    }
}
