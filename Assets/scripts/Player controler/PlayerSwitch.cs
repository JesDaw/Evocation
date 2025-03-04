using UnityEngine;

public class PlayerSwitch : MonoBehaviour
{
    public PlayerMovement playerController;
    public PlayerMovement player2Controller;
    public Camera player1_cam;
    public Camera player2_cam;
    public bool player1Active = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightShift))
        {
            SwitchPlayer();
        }
    }

    public void SwitchPlayer()
    {
        if (player1Active)
        {
            playerController.enabled = false;
            player2Controller.enabled = true;
            player1_cam.enabled = false;
            player2_cam.enabled = true;
            player1Active = false;
        }
        else
        {
            playerController.enabled = true;
            player2Controller.enabled = false;
            player1_cam.enabled = true;
            player2_cam.enabled = false;
            player1Active = true;
        }
    }
}
