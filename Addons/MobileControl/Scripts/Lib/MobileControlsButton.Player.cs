using Mirror;
using UnityEngine;

// Player
partial class Player
{
    [HideInInspector] public bool runButtonPressed = false;
    [HideInInspector] public bool targetButtonPressed = false;
    [HideInInspector] public bool jumpButtonPressed = false;
    [HideInInspector] public bool swithButtonPressed = false;

    // simple tab targeting for buttons
    [Client]
    public void TargetNearestButton()
    {
       targetButtonPressed = true;
    }

    [Client]
    public void jumpNearestButton()
    {
        jumpButtonPressed = true;
    }
}