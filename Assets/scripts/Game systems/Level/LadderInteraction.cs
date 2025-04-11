using UnityEngine;

public class LadderInteraction : MonoBehaviour
{
    [SerializeField] private ActivePlayer activePlayer;
    [SerializeField] private Transform interactableTop;
    [SerializeField] private Transform interactableBottom;

    public void ToggleClimbingTop()
    {
        var PlayerMovement = activePlayer.CurrentPlayer.GetComponent<PlayerMovement>();
        if (PlayerMovement.isClimbing) return;
        Transform playerTransform = activePlayer.CurrentPlayer.transform;
        Vector2 newPosition = new Vector2(interactableTop.position.x, interactableTop.position.y);
        playerTransform.position = newPosition;
        PlayerMovement.ToggleClimbing();
    }

    public void ToggleClimbingBottom()
    {
        Transform playerTransform = activePlayer.CurrentPlayer.transform;
        Vector2 newPosition = new Vector2(interactableBottom.position.x, interactableBottom.position.y);
        playerTransform.position = newPosition;
        activePlayer.CurrentPlayer.GetComponent<PlayerMovement>().ToggleClimbing();
    }
}
