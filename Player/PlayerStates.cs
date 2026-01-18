using UnityEngine;
using System;
using Mirror;

namespace uMMORPG
{
    public partial class Player
    {
        [Server]
        string UpdateServer_IDLE()
        {
            if (EventDied())
            {
                return "DEAD";
            }
            if (EventStunned())
            {
                movement.Reset();
                return "STUNNED";
            }
            if (EventCancelAction())
            {
                target = null;
                return "IDLE";
            }
            if (EventTradeStarted())
            {
                skills.CancelCast(); // just in case
                target = trading.FindPlayerFromInvitation();
                return "TRADING";
            }
            if (EventCraftingStarted())
            {
                skills.CancelCast(); // just in case
                return "CRAFTING";
            }
            if (EventMoveStart())
            {
                skills.CancelCast();
                return "MOVING";
            }
            if (EventSkillRequest())
            {
                if (!mountControl.IsMounted())
                {
                    Skill skill = skills.skills[skills.currentSkill];
                    nextTarget = target; // return to this one after any corrections by CastCheckTarget
                    if (skills.CastCheckSelf(skill) &&
                        skills.CastCheckTarget(skill) &&
                        skills.CastCheckDistance(skill, out Vector3 destination))
                    {
                        movement.Reset();
                        skills.StartCast(skill);
                        return "CASTING";
                    }
                    else
                    {
                        skills.currentSkill = -1;
                        nextTarget = null; // nevermind, clear again (otherwise it's shown in UITarget)
                        return "IDLE";
                    }
                }
            }
            if (EventSkillFinished()) {} // don't care
            if (EventMoveEnd()) {} // don't care
            if (EventTradeDone()) {} // don't care
            if (EventCraftingDone()) {} // don't care
            if (EventRespawn()) {} // don't care
            if (EventTargetDied()) {} // don't care
            if (EventTargetDisappeared()) {} // don't care

            return "IDLE"; // nothing interesting happened
        }

        [Server]
        string UpdateServer_MOVING()
        {
            if (EventDied())
            {
                return "DEAD";
            }
            if (EventStunned())
            {
                movement.Reset();
                return "STUNNED";
            }
            if (EventMoveEnd())
            {
                return "IDLE";
            }
            if (EventCancelAction())
            {
                skills.CancelCast();
                return "IDLE";
            }
            if (EventTradeStarted())
            {
                skills.CancelCast();
                movement.Reset();
                target = trading.FindPlayerFromInvitation();
                return "TRADING";
            }
            if (EventCraftingStarted())
            {
                skills.CancelCast();
                movement.Reset();
                return "CRAFTING";
            }
            if (EventSkillRequest())
            {
                if (!mountControl.IsMounted())
                {
                    Skill skill = skills.skills[skills.currentSkill];
                    if (skills.CastCheckSelf(skill) &&
                        skills.CastCheckTarget(skill) &&
                        skills.CastCheckDistance(skill, out Vector3 destination))
                    {
                        skills.StartCast(skill);
                        return "CASTING";
                    }
                }
            }
            if (EventMoveStart()) {} // don't care
            if (EventSkillFinished()) {} // don't care
            if (EventTradeDone()) {} // don't care
            if (EventCraftingDone()) {} // don't care
            if (EventRespawn()) {} // don't care
            if (EventTargetDied()) {} // don't care
            if (EventTargetDisappeared()) {} // don't care

            return "MOVING"; // nothing interesting happened
        }

        void UseNextTargetIfAny()
        {
            if (nextTarget != null)
            {
                target = nextTarget;
                nextTarget = null;
            }
        }

        [Server]
        string UpdateServer_CASTING()
        {
            if (target && movement.DoCombatLookAt())
                movement.LookAtY(target.transform.position);

            if (EventDied())
            {
                UseNextTargetIfAny(); // if user selected a new target while casting
                return "DEAD";
            }
            if (EventStunned())
            {
                skills.CancelCast(!continueCastAfterStunned);
                movement.Reset();
                return "STUNNED";
            }
            if (EventMoveStart())  {  }
            if (EventCancelAction())
            {
                skills.CancelCast();
                UseNextTargetIfAny(); // if user selected a new target while casting
                return "IDLE";
            }
            if (EventTradeStarted())
            {
                skills.CancelCast();
                movement.Reset();
                target = trading.FindPlayerFromInvitation();
                nextTarget = null;
                return "TRADING";
            }
            if (EventTargetDisappeared())
            {
                if (skills.skills[skills.currentSkill].cancelCastIfTargetDied)
                {
                    skills.CancelCast();
                    UseNextTargetIfAny(); // if user selected a new target while casting
                    return "IDLE";
                }
            }
            if (EventTargetDied())
            {
                if (skills.skills[skills.currentSkill].cancelCastIfTargetDied)
                {
                    skills.CancelCast();
                    UseNextTargetIfAny(); // if user selected a new target while casting
                    return "IDLE";
                }
            }
            if (EventSkillFinished())
            {
                Skill skill = skills.skills[skills.currentSkill];
                skills.FinishCast(skill);
                skills.currentSkill = -1;
                UseNextTargetIfAny();
                return "IDLE";
            }
            if (EventMoveEnd()) {} // don't care
            if (EventTradeDone()) {} // don't care
            if (EventCraftingStarted()) {} // don't care
            if (EventCraftingDone()) {} // don't care
            if (EventRespawn()) {} // don't care
            if (EventSkillRequest()) {} // don't care

            return "CASTING"; // nothing interesting happened
        }

        [Server]
        string UpdateServer_STUNNED()
        {
            if (EventDied())
            {
                return "DEAD";
            }
            if (EventStunned())
            {
                return "STUNNED";
            }

            return "IDLE";
        }

        [Server]
        string UpdateServer_TRADING()
        {
            if (EventDied())
            {
                trading.Cleanup();
                return "DEAD";
            }
            if (EventStunned())
            {
                skills.CancelCast();
                movement.Reset();
                trading.Cleanup();
                return "STUNNED";
            }
            if (EventMoveStart())
            {
                movement.Reset();
                return "TRADING";
            }
            if (EventCancelAction())
            {
                trading.Cleanup();
                return "IDLE";
            }
            if (EventTargetDisappeared())
            {
                trading.Cleanup();
                return "IDLE";
            }
            if (EventTargetDied())
            {
                trading.Cleanup();
                return "IDLE";
            }
            if (EventTradeDone())
            {
                trading.Cleanup();
                return "IDLE";
            }
            if (EventMoveEnd()) {} // don't care
            if (EventSkillFinished()) {} // don't care
            if (EventCraftingStarted()) {} // don't care
            if (EventCraftingDone()) {} // don't care
            if (EventRespawn()) {} // don't care
            if (EventTradeStarted()) {} // don't care
            if (EventSkillRequest()) {} // don't care

            return "TRADING"; // nothing interesting happened
        }

        [Server]
        string UpdateServer_CRAFTING()
        {

            if (EventDied())
            {
                return "DEAD";
            }
            if (EventStunned())
            {
                movement.Reset();
                return "STUNNED";
            }
            if (EventMoveStart())
            {
                movement.Reset();
                return "CRAFTING";
            }
            if (EventCraftingDone())
            {
                crafting.Craft();
                return "IDLE";
            }
            if (EventCancelAction()) {} // don't care. user pressed craft, we craft.
            if (EventTargetDisappeared()) {} // don't care
            if (EventTargetDied()) {} // don't care
            if (EventMoveEnd()) {} // don't care
            if (EventSkillFinished()) {} // don't care
            if (EventRespawn()) {} // don't care
            if (EventTradeStarted()) {} // don't care
            if (EventTradeDone()) {} // don't care
            if (EventCraftingStarted()) {} // don't care
            if (EventSkillRequest()) {} // don't care

            return "CRAFTING"; // nothing interesting happened
        }

        [Server]
        string UpdateServer_DEAD()
        {
            if (EventRespawn())
            {
                Transform start = NetworkManagerMMO.GetNearestStartPosition(transform.position);
                movement.Warp(start.position);
                Revive(0.5f);
                return "IDLE";
            }
            if (EventMoveStart())
            {
                Debug.LogWarning("Player " + name + " moved while dead. This should not happen.");
                return "DEAD";
            }
            if (EventMoveEnd()) {} // don't care
            if (EventSkillFinished()) {} // don't care
            if (EventDied()) {} // don't care
            if (EventCancelAction()) {} // don't care
            if (EventTradeStarted()) {} // don't care
            if (EventTradeDone()) {} // don't care
            if (EventCraftingStarted()) {} // don't care
            if (EventCraftingDone()) {} // don't care
            if (EventTargetDisappeared()) {} // don't care
            if (EventTargetDied()) {} // don't care
            if (EventSkillRequest()) {} // don't care

            return "DEAD"; // nothing interesting happened
        }

        [Server]
        protected override string UpdateServer()
        {

            if (state == "IDLE")     return UpdateServer_IDLE();
            if (state == "MOVING")   return UpdateServer_MOVING();
            if (state == "CASTING")  return UpdateServer_CASTING();
            if (state == "STUNNED")  return UpdateServer_STUNNED();
            if (state == "TRADING")  return UpdateServer_TRADING();
            if (state == "CRAFTING") return UpdateServer_CRAFTING();
            if (state == "DEAD")     return UpdateServer_DEAD();
            Debug.LogError("invalid state:" + state);
            return "IDLE";
        }

    }
}
