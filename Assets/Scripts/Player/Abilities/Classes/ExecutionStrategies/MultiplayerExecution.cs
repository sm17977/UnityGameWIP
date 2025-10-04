using Unity.Netcode;
using UnityEngine;

public class NetcodeExecution : IExecutionStrategy {
    
    private RPCController _rpcController;
    private GameObject _player;
    
    public void Execute(Ability data, CastContext ctx, ICastBehaviour behavior) {
        _player = ctx.PlayerController.gameObject;
        _rpcController = _player.GetComponent<RPCController>();
        
        // Client side cast
        behavior.Cast(data, ctx, ctx.PlayerController);
        
        // Server side spawn
        var playerNetworkBehaviour = _player.GetComponent<NetworkBehaviour>();
        var localClientId = playerNetworkBehaviour.NetworkManager.LocalClientId;
        _rpcController.SpawnProjectileServerRpc(ctx.Direction, ctx.SpawnPos, ctx.TargetPos, localClientId, data.key);
    }

    public void Recast(Ability data, GameObject activeObject, ICastBehaviour behavior) {
        behavior.Recast(data, activeObject);
        _rpcController.RecastAbilityServerRpc(data.key);
    }
}

