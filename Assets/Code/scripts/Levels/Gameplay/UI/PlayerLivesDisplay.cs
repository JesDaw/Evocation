using UnityEngine;
using System.Collections.Generic;

public class PlayerLivesDisplay : MonoBehaviour
{
    [SerializeField] PlayerLivesManager livesManager;
    [SerializeField] List<GameObject> torchIcons = new List<GameObject>();
    private int lastMaxLives = -1;

    void Start()
    {
        UpdateTorchDisplay();
    }

    void Update()
    {
        if (livesManager != null && livesManager.MaxLives != lastMaxLives)
        {
            UpdateTorchDisplay();
        }
    }

    void UpdateTorchDisplay()
    {
        if (livesManager == null) return;

        lastMaxLives = livesManager.MaxLives;

        for (int i = 0; i < torchIcons.Count; i++)
        {
            if (torchIcons[i] != null)
            {
                torchIcons[i].SetActive(i < livesManager.MaxLives);
            }
        }
    }
}
