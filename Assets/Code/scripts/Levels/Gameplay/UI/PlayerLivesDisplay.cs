using UnityEngine;
using System.Collections.Generic;

public class PlayerLivesDisplay : MonoBehaviour
{
    [SerializeField] PlayerLivesManager livesManager;
    [SerializeField] List<GameObject> torchIcons = new List<GameObject>();
    [SerializeField] List<GameObject> Fire = new List<GameObject>();
    void Start()
    {
        UpdateTorchDisplay();
    }

    public void UpdateTorchDisplay()
    {
        if (livesManager == null) return;

        for (int i = 0; i < torchIcons.Count; i++)
        {
            if (torchIcons[i] != null)
            {
                Fire[i].SetActive(i < livesManager.MaxLives);
            }
        }
    }
}
