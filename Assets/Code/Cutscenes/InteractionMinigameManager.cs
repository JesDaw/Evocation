using UnityEngine;

public class InteractionMinigameManager : MonoBehaviour
{
    public UltEvents.UltEvent StartingEvent;
    public static InteractionMinigameManager Instance { get; private set; }
    [SerializeField] GameObject[] Bondies;
    [SerializeField] GameObject[] faces;
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        StartingEvent?.Invoke();
    }

    public void SetCharacterBody(int id)
    {
        for (int v = 0; v < Bondies.Length; v++)
        {
            if (v != id)
            {
                Bondies[v].gameObject.SetActive(false);
            }
            else
            {
                Bondies[v].gameObject.SetActive(true);
            }
        }
    }
    public void SetCharacterFace(int id)
    {
        for (int v = 0; v< faces.Length; v++)
        {
            if (v != id)
            {
                faces[v].gameObject.SetActive(false);
            }
            else
            {
                faces[v].gameObject.SetActive(true);
            }
        }
    }
}
