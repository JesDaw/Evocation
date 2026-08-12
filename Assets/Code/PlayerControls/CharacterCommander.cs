using System;
using System.Collections.Generic;
using System.Data;
using System.Numerics;
//using Codice.Client.BaseCommands;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;
using Vector2 = UnityEngine.Vector2;

/// <summary>
/// General class to manage 'continuous' and 'discrete' commands for a game character.
/// 
/// A 'continuous' command is one that is active as long as a key is pressed or an
/// input action has not been canceled - like 'move'.  They may often be superceded by
/// 'discrete' commands.  We just need to keep track of whether or not a continous
/// command is 'active' and not concern ourselves with how many times the command has been
/// given.
/// 
/// A 'discrete' command is one that is made with every key press or every time a
/// particular input action is 'performed' - like 'attack'.  These commands are queued
/// since the order may be important.
/// 
/// <typeparam name="C">enum of the various Continuous commands supported </typeparam>
/// <typeparam name="D">enum of the various Discrete commands supported</typeparam>
/// <typeparam name="CTX">struct to hold any interesting data associated with all possible commands</typeparam>
/// </summary>
public class BaseCharacterCommander<C, D, CTX>
where C : System.Enum
where D : System.Enum
where CTX : struct
{
    // Holds active 'continuous' commands
    Dictionary<C, CTX?> activeCmds = new Dictionary<C, CTX?>();

    // Holds queues 'discrete' commands
    LinkedList<(D, CTX?)> pendingCmds = new LinkedList<(D, CTX?)>();

    //===============================
    // Command "Sender" Methods
    //===============================

    /// <summary>
    /// Activate or deactive a continuous commands
    /// </summary>
    /// <param name="cmd">command type</param>
    /// <param name="isActive">activation status flag</param>
    /// <param name="cmdData">Any associated command data</param>
    public void SetActiveCmd(C cmd, bool isActive, CTX? cmdData)
    {
        if (isActive)
        {
            activeCmds[cmd] = cmdData;
        }
        else
        {
            activeCmds.Remove(cmd);
        }
    }

    /// <summary>
    /// Queue a discrete command
    /// </summary>
    /// <param name="cmd">The command type</param>
    /// <param name="cmdData">Any associated command data</param>
    public void SendCmd(D cmd, CTX? cmdData)
    {
        pendingCmds.AddLast((cmd, cmdData));
    }

    //===============================
    // Command "Receiver" Methods
    //===============================

    /// <summary>
    /// Returns if the specified command is active along with any associate data
    /// </summary>
    /// <param name="cmd">Command type</param>
    /// <param name="data">out parameter for any associated data</param>
    /// <returns>active flag</returns>
    public bool IsCmdActive(C cmd, out Nullable<CTX> data)
    {
        if (activeCmds.ContainsKey(cmd))
        {
            data = activeCmds[cmd];
            return true;
        }

        data = null;
        return false;
    }

    /// <summary>
    /// Returns if the specified command is active
    /// </summary>
    /// <param name="cmd">Command type</param>
    /// <returns>active flag</returns>
    public bool IsCmdActive(C cmd)
    {
        CTX? data;

        var result = IsCmdActive(cmd, out data);

        return result;
    }

    /// <summary>
    /// Returns if any continous command is active
    /// </summary>
    /// <returns>active flag</returns>
    public bool IsCmdActive()
    {
        return (activeCmds.Count != 0);
    }

    /// <summary>
    /// Returns if a specified command type is on the queue
    /// </summary>
    /// <param name="cmd">command type</param>
    /// <returns>'true' if on the queue, 'false' if not</returns>
    public bool IsCmdPending(D cmd)
    {
        foreach (var c in pendingCmds)
        {
            if (c.Item1.Equals(cmd))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns if any discrete commands are pending (i.e. on the queue)
    /// </summary>
    /// <returns>'true' if a command is pending, 'false' if not</returns>
    public bool IsCmdPending()
    {
        // Returns 'true' if any discrete command is pending
        return pendingCmds.Count != 0;
    }

    /// <summary>
    /// Remove the next instance of a command from the queue
    /// </summary>
    /// <param name="cmd">command type</param>
    /// <returns>tuple -> (command type, any associated data)</returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no instance of the specified command is on the queue
    /// </exception>
    public (D, CTX?) TakePendingCmd(D cmd)
    {
        if (pendingCmds.Count > 0)
        {
            var currNode = pendingCmds.First;
            while (currNode != null)
            {
                if (currNode.Value.Item1.Equals(cmd))
                {
                    var result = currNode.Value;
                    pendingCmds.Remove(currNode);
                    return result;
                }
                currNode = currNode.Next;
            }
        }

        throw new KeyNotFoundException($"No {cmd} command was pending");
    }

    /// <summary>
    /// Remove the next pending action (of any type) off the queue
    /// </summary>
    /// <returns>tuple -> (command type, any associated data)</returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no instance of the specified command is on the queue
    /// </exception>
    public (D, CTX?) TakePendingCmd()
    {
        if (pendingCmds.Count > 0)
        {
            var node = pendingCmds.First;
            pendingCmds.RemoveFirst();
            return node.Value;
        }

        throw new KeyNotFoundException($"No command was pending");
    }

    /// <summary>
    /// Returns the type of the next pending command on the queue
    /// </summary>
    /// <returns>command type</returns>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the pending queueis empty
    /// </exception>
    public D PeekPendingCmd()
    {
        if (pendingCmds.Count > 0)
        {
            return pendingCmds.First.Value.Item1;
        }

        throw new KeyNotFoundException($"No command was pending");
    }

    /// <summary>
    /// Remove all instances of a particular command from the queue.
    /// 
    /// Useful to control the amount of buffering allowed for a givem command
    /// </summary>
    /// <param name="cmd">command type</param>
    public void ClearPendingCmds(D cmd)
    {
        var currNode = pendingCmds.First;
        while (currNode != null)
        {
            if (currNode.Value.Item1.Equals(cmd))
            {
                var nextNode = currNode.Next;
                pendingCmds.Remove(currNode);
                currNode = nextNode;
            }
            else
            {
                currNode = currNode.Next;
            }
        }
    }

    /// <summary>
    /// Remove all pending commands
    /// </summary>
    public void ClearPendingCmds()
    {
        pendingCmds.Clear();
    }

    public void ClearAllCommands()
    {
        activeCmds.Clear();
        pendingCmds.Clear();
    }

}


/// <summary>
/// Enum of the valid Continuous commands for a Player Character
/// </summary>
public enum ContinuousPlayerCommand
{
    Move,
    Climb,
}

/// <summary>
/// Enum of the valid discrete commands for a player character
/// </summary>
public enum DiscretePlayerCommand
{
    Attack,
    KnockBack,
}

/// <summary>
/// Structure that holds any interesting data associated with Player character commands
/// </summary>
public struct PlayerCommandData
{
    public PlayerCommandData(bool asButton)
    {
        AsButton = asButton;
        AsVector2 = null;
    }

    public PlayerCommandData(Vector2 asVector2)
    {
        AsVector2 = asVector2;
        AsButton = false;
    }

    public bool AsButton { get; }
    public Vector2? AsVector2 { get; }
}

/// <summary>
/// BaseCharacterCommander specialized for Player Characters
/// </summary>
/// <summary>
/// BaseCharacterCommander specialized for Player Characters
/// </summary>
public class PlayerCommander :
    BaseCharacterCommander<
        ContinuousPlayerCommand,
        DiscretePlayerCommand,
        PlayerCommandData
    >
{
    bool _isFreeCamActive;

    public PlayerCommander(bool isFreeCamActive)
    {
        _isFreeCamActive = isFreeCamActive;
    }

    /// <summary>
    /// Handle 'Move' Input Action
    /// </summary>
    /// <param name="context">context associated with the event</param>
    public void OnMove(InputAction.CallbackContext context)
    {
        if ((context.ReadValue<Vector2>() == Vector2.zero) || context.canceled)
        {
            SetActiveCmd(ContinuousPlayerCommand.Move, false, null);
        }
        else
        {
            SetActiveCmd(
                ContinuousPlayerCommand.Move,
                true,
                new PlayerCommandData(context.ReadValue<Vector2>()));
        }
    }


    public void OnAttack(InputAction.CallbackContext context)
    {        
        if (context.performed && context.ReadValueAsButton())
        {
            if (!IsCmdPending(DiscretePlayerCommand.Attack))
            {
                SendCmd(DiscretePlayerCommand.Attack, null);
            }
        }
    }

    public void OnToggleFreeCam(InputAction.CallbackContext context)
    {
        if (!_isFreeCamActive)
        {
            // Deactivate all potentially active continuous commands
            foreach (ContinuousPlayerCommand c in Enum.GetValues(typeof(ContinuousPlayerCommand)))
            {
                SetActiveCmd(c, false, null);
            }
        }
        _isFreeCamActive = !_isFreeCamActive;
    }

    /// <summary>
    /// Handle a 'KnockBack' event
    /// </summary>
    public void OnKnockBack()
    {
        SendCmd(DiscretePlayerCommand.KnockBack, null);
    }

    /// <summary>
    /// Are any commands active or pending?
    /// </summary>
    /// <returns>'true' if NO commands are active or pending, 'false' if not</returns>
    public bool IsIdle()
    {
        return !IsCmdActive() && !IsCmdPending();
    }
}