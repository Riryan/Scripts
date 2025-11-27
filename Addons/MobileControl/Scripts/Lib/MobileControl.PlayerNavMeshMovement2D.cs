#if _iMMO2D
using Mirror;
using UnityEngine;

public partial class PlayerNavMeshMovement2D
{

    [Client]
    void MoveJoystick()
    {
        // don't move if currently typing in an input
        // we check this after checking h and v to save computations
        if (!UIUtils.AnyInputActive() && !disableJoystickMove)
        {
            Vector2 mobile = MobileControls.getJoystickAxis();
            // get input direction while alive and while not typing in chat
            // (otherwise 0 so we keep falling even if we die while jumping etc.)
            //Ici on récupere la position du joystick
            bool autorun = MobileControls.GetAutorun();

            // get horizontal and vertical input
            // 'raw' to start moving immediately. otherwise too much delay.
            // note: no != 0 check because it's 0 when we stop moving rapidly
            float horizontal = (autorun && mobile.x ==0) ? player.lookDirection.x : mobile.x;//  Input.GetAxisRaw("Horizontal");
            float vertical = (autorun && mobile.y == 0) ? player.lookDirection.y : mobile.y; // Input.GetAxisRaw("Vertical");
            
            

            if (horizontal != 0 || vertical != 0)
            {
                // create direction, normalize in case of diagonal movement
                Vector2 direction = new Vector2(horizontal, vertical);
                if (direction.magnitude > 1) direction = direction.normalized;

                // draw direction for debugging
                Debug.DrawLine(transform.position, transform.position + (Vector3)direction, Color.green, 0, false);

                // clear indicator if there is one, and if it's not on a target
                // (simply looks better)
                if (direction != Vector2.zero)
                    indicator.ClearIfNoParent();

                // cancel path if we are already doing click movement, otherwise
                // we will slide
                agent.ResetMovement();

                // note: SetSpeed() already sets agent.speed to player.speed
                agent.velocity = direction * agent.speed;

                // clear requested skill in any case because if we clicked
                // somewhere else then we don't care about it anymore
                player.useSkillWhenCloser = -1;
            }
        }
    }
}
#endif
