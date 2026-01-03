using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;

public class PlayerLivesDisplay : MonoBehaviour
{
    [SerializeField] PlayerLivesManager livesManager;
    [SerializeField] List<GameObject> torchIcons = new List<GameObject>();
    [SerializeField] List<GameObject> fire = new List<GameObject>();
    [SerializeField] IntVeriable LifeCount;
    void Start()
    {
        UpdateMaxLivesDesplay();
        foreach (GameObject flame in fire) flame.SetActive(false);
        UpdateTorchDisplay();
    }

    void UpdateMaxLivesDesplay()
    {
        foreach (GameObject torch in torchIcons) torch.SetActive(false);
        for (int i = 0; i < livesManager.MaxLives; i++)
        {
            if (torchIcons[i] != null)
            {
                torchIcons[i].SetActive(true);
            }
            else
            {
                UnityEngine.Debug.LogError($"Icon count: {torchIcons.Count} but maxlives count {livesManager.MaxLives}");
            }
        }
    }
    public void UpdateTorchDisplay()
    {
        if (livesManager == null) 
        {
            UnityEngine.Debug.LogWarning($"Player lives desplay on {gameObject.name} doent have a referance to the livesManager set");
            return;
        }

        for (int i = 0; i < fire.Count; i++)
        {
            if (fire[i] != null)
            {
                fire[i].SetActive(i < LifeCount._Value);
            }
        }
    }
}
