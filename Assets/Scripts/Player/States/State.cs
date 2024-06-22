
using Unity.Netcode;
using UnityEngine;

public abstract class State {
    public abstract void Enter();
    public abstract void Execute();
    public abstract void Exit();
   
}
