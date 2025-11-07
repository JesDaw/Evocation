using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class CharacterCommanderTest
{
    enum CCmd
    {
        Move,
        Climb,
    }

    enum DCmd
    {
        Attack,
        Jump,
    }

    public struct CmdData
    {
        public CmdData(int anInt)
        {
            AnInt = anInt;
        }

        public int AnInt { get; }
    }

    BaseCharacterCommander<CCmd, DCmd, CmdData> testObj;

    [SetUp]
    public void SetupTestObj()
    {
        testObj = new BaseCharacterCommander<CCmd, DCmd, CmdData>();
    }

    // A Test behaves as an ordinary method
    [Test]
    public void InitialStateIsIdle()
    {
        Assert.IsFalse(testObj.IsCmdActive());
        Assert.IsFalse(testObj.IsCmdPending());
    }

    [Test]
    public void SingleContinousCommandWithoutDataIsActive()
    {
        testObj.SetActiveCmd(CCmd.Move, true, null);
        Assert.IsTrue(testObj.IsCmdActive());
        CmdData? data;
        Assert.IsTrue(testObj.IsCmdActive(CCmd.Move, out data));
        Assert.IsNull(data);
    }

    [Test]
    public void SingleContinousCommandWithDataIsActive()
    {
        CmdData someData = new CmdData(3);
        testObj.SetActiveCmd(CCmd.Move, true, someData);
        Assert.IsTrue(testObj.IsCmdActive());
        CmdData? data;
        Assert.IsTrue(testObj.IsCmdActive(CCmd.Move, out data));
        Assert.AreEqual(data, someData);
    }

    [Test]
    public void DualContinousCommandsAreActive()
    {
        CmdData d1 = new CmdData(1);
        CmdData d2 = new CmdData(2);
        testObj.SetActiveCmd(CCmd.Move, true, d1);
        testObj.SetActiveCmd(CCmd.Climb, true, d2);
        Assert.IsTrue(testObj.IsCmdActive());

        CmdData? data;
        Assert.IsTrue(testObj.IsCmdActive(CCmd.Move, out data));
        Assert.AreEqual(data, d1);

        Assert.IsTrue(testObj.IsCmdActive(CCmd.Climb, out data));
        Assert.AreEqual(data, d2);
    }

    [Test]
    public void DualContinousCommandsWithSingleToggle()
    {
        testObj.SetActiveCmd(CCmd.Move, true, null);
        testObj.SetActiveCmd(CCmd.Climb, true, null);
        testObj.SetActiveCmd(CCmd.Climb, false, null);

        Assert.IsTrue(testObj.IsCmdActive());

        CmdData? data;
        Assert.IsTrue(testObj.IsCmdActive(CCmd.Move, out data));
        Assert.IsFalse(testObj.IsCmdActive(CCmd.Climb, out data));
    }

    [Test]
    public void SingleDiscreteCommandIsPending()
    {
        testObj.SendCmd(DCmd.Attack, null);
        Assert.IsTrue(testObj.IsCmdPending());

        Assert.IsTrue(testObj.IsCmdPending(DCmd.Attack));
        Assert.IsFalse(testObj.IsCmdPending(DCmd.Jump));
    }

    [Test]
    public void SingleDiscreteCommandIsTaken()
    {
        testObj.SendCmd(DCmd.Attack, null);
        var c = testObj.TakePendingCmd(DCmd.Attack);
        Assert.AreEqual(c.Item1, DCmd.Attack);
        Assert.IsNull(c.Item2);
        Assert.IsFalse(testObj.IsCmdPending());
    }

    [Test]
    public void SinglePendingCmdWhenNextCommandIsTaken()
    {
        testObj.SendCmd(DCmd.Attack, null);
        var c = testObj.TakePendingCmd();
        Assert.AreEqual(c.Item1, DCmd.Attack);
        Assert.IsNull(c.Item2);
    }

    [Test]
    public void TwoPendingCmdsWhenLaterCommandIsTaken()
    {
        CmdData d1 = new CmdData(1);
        testObj.SendCmd(DCmd.Attack, null);
        testObj.SendCmd(DCmd.Jump, d1);
        var c = testObj.TakePendingCmd(DCmd.Jump);
        Assert.AreEqual(c.Item1, DCmd.Jump);
        Assert.AreEqual(c.Item2, d1);
        Assert.IsTrue(testObj.IsCmdPending());
    }


    [Test]
    public void TwoPendingCmdsWhenTakenInOrder()
    {
        CmdData d1 = new CmdData(1);
        testObj.SendCmd(DCmd.Attack, null);
        testObj.SendCmd(DCmd.Jump, d1);

        var c = testObj.TakePendingCmd();
        Assert.AreEqual(c.Item1, DCmd.Attack);
        Assert.IsNull(c.Item2);

        c = testObj.TakePendingCmd();
        Assert.AreEqual(c.Item1, DCmd.Jump);
        Assert.AreEqual(c.Item2, d1);

        Assert.IsFalse(testObj.IsCmdPending());
    }

    [Test]
    public void TwoPendingCmdsWhenOneIsCleared()
    {
        testObj.SendCmd(DCmd.Attack, null);
        testObj.SendCmd(DCmd.Jump, null);

        testObj.ClearPendingCmds(DCmd.Attack);

        Assert.IsTrue(testObj.IsCmdPending(DCmd.Jump));
        Assert.IsFalse(testObj.IsCmdPending(DCmd.Attack));
    }

    [Test]
    public void TwoPendingCmdsWhenAllAreCleared()
    {
        testObj.SendCmd(DCmd.Attack, null);
        testObj.SendCmd(DCmd.Jump, null);

        testObj.ClearPendingCmds();

        Assert.IsFalse(testObj.IsCmdPending(DCmd.Jump));
        Assert.IsFalse(testObj.IsCmdPending(DCmd.Attack));
        Assert.IsFalse(testObj.IsCmdPending());
    }

    [Test]
    public void NoPendingCmdsWhenOneIsTaken()
    {
        Assert.Throws<KeyNotFoundException>(() => testObj.TakePendingCmd(DCmd.Attack));
    }

    [Test]
    public void NoPendingCmdsWhenNextIsTaken()
    {
        Assert.Throws<KeyNotFoundException>(() => testObj.TakePendingCmd());
    }

    [Test]
    public void NoPendingCmdsWhenPeekIsPerformed()
    {
        Assert.Throws<KeyNotFoundException>(() => testObj.PeekPendingCmd());
    }

    [Test]
    public void OnePendingCmdWhenPeekIsPerformed()
    {
        testObj.SendCmd(DCmd.Attack, null);
        Assert.AreEqual(DCmd.Attack, testObj.PeekPendingCmd());
    }
}
