using UnityEngine;
using DG.Tweening;

public class PlayerScarf : MonoBehaviour 
{
    [SerializeField] SpriteRenderer[] scarfPieces;
    [SerializeField] float fadeDuration = 1f;
    bool scarfIsDesplayed = false;

    void FadeScarf(bool In) 
    {
        int targetAlpha = In ? 1 : 0;
        foreach (var piece in scarfPieces) piece.DOFade(targetAlpha, fadeDuration);
    }

    void Update()
    {
        if (scarfPieces.Length <= 0) return;
        if (gameObject == ActivePlayer.Instance.CurrentPlayer && !CameraControlSwitcher.Instance.FreeCamIsActive && !scarfIsDesplayed)
        {
            FadeScarf(true);
            scarfIsDesplayed = true;
        }
        else if ((gameObject != ActivePlayer.Instance.CurrentPlayer || CameraControlSwitcher.Instance.FreeCamIsActive) && scarfIsDesplayed)
        {
            FadeScarf(false);
            scarfIsDesplayed = false;
        }
    }
}