using System.Collections.Generic;
using Kart;
using UnityEngine;
using Unity.Netcode;

public class NetworkStateManager : NetworkBehaviour {

    private RPCController _rpc;
    private InputProcessor _inputProcessor;
    
    public NetworkTimer NetworkTimer;
    private CircularBuffer<InputPayload> clientInputBuffer;
    private CircularBuffer<StatePayload> clientStateBuffer;
    public Queue<InputPayload> serverInputQueue;
    private CircularBuffer<StatePayload> serverStateBuffer;

    public StatePayload lastServerState;
    public InputPayload lastProcessedState;

    private const int k_bufferSize = 1024;
    private readonly float reconciliationThreshold = 0.5f;

    private void Awake() {
        NetworkTimer = new NetworkTimer(50);
        clientInputBuffer = new CircularBuffer<InputPayload>(k_bufferSize);
        clientStateBuffer = new CircularBuffer<StatePayload>(k_bufferSize);
        serverStateBuffer = new CircularBuffer<StatePayload>(k_bufferSize);
        serverInputQueue = new Queue<InputPayload>();

        _rpc = GetComponent<RPCController>();
        _inputProcessor = GetComponent<InputProcessor>();
    }

    private void Update() {
        NetworkTimer.Update(Time.deltaTime);
    }

    private void FixedUpdate() {
        while (NetworkTimer.ShouldTick()) {
            HandleClientTick();
            HandleServerTick();
        }
    }

    private void HandleClientTick() {
        if (!IsClient || !IsOwner) return;

        var currentTick = NetworkTimer.CurrentTick;
        var bufferIndex = currentTick % k_bufferSize;
        
        InputPayload inputPayload = new InputPayload {
            Tick = currentTick,
            TargetPosition = _inputProcessor.lastClickPosition,
            AbilityKey = "Q"
        };
        
        clientInputBuffer.Add(inputPayload, bufferIndex);
        _rpc.SendInputRpc(inputPayload);
        
        clientStateBuffer.Add(new StatePayload {
            Position = transform.position,
            TargetPosition = _inputProcessor.lastClickPosition
        }, bufferIndex);
    }

    private void HandleServerTick() {
        if (!IsServer) return;
        var bufferIndex = -1;

        while (serverInputQueue.Count > 0) {
            var inputPayload = serverInputQueue.Dequeue();
            bufferIndex = inputPayload.Tick % k_bufferSize;
            
            StatePayload statePayload = new StatePayload {
                Position = transform.position,
                TargetPosition = inputPayload.TargetPosition
            };
            serverStateBuffer.Add(statePayload, bufferIndex);
        }

        if (bufferIndex != -1) {
            _rpc.SendStateRpc(serverStateBuffer.Get(bufferIndex));
        }
    }

    // Called by InputProcessor when player moves
    public void SendMoveCommand(Vector3 position) {
        InputPayload payload = new InputPayload {
            Tick = NetworkTimer.CurrentTick,
            TargetPosition = position,
            AbilityKey = "X"
        };
        clientInputBuffer.Add(payload, payload.Tick % k_bufferSize);
        _rpc.SendInputRpc(payload);
    }

    // public void SendAttackCommand(Vector3 position) {
    //     InputPayload payload = new InputPayload {
    //         Tick = NetworkTimer.CurrentTick,
    //         TargetPosition = position,
    //         AbilityKey = "Attack"
    //     };
    //     clientInputBuffer.Add(payload, payload.Tick % k_bufferSize);
    //     _rpc.SendInputRpc(payload);
    // }

    public void SendSpellCommand(string key, Ability ability) {
        // InputPayload payload = new InputPayload {
        //     Tick = NetworkTimer.CurrentTick,
        //     TargetPosition = Vector3.zero,
        //     AbilityKey = key
        // };
        // clientInputBuffer.Add(payload, payload.Tick % k_bufferSize);
        // _rpc.SendInputRpc(payload);
    }
}
