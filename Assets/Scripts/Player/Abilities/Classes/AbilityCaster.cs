using Unity.Netcode;
using UnityEngine;


public class AbilityCaster {
    private readonly LuxPlayerController _playerController;
    private RPCController _rpcController;
    private readonly bool _isMultiplayer;

    public AbilityCaster(LuxPlayerController playerController, bool isMultiplayer) {
        _playerController = playerController;
        _isMultiplayer = isMultiplayer;
    }

    public void Cast(Ability ability, CastContext ctx) {

        // Client side cast
        var prefab = SpawnAbilityPrefab(ability, ctx);
        InitAbilityScript(ability, ctx, prefab);

        if (!_isMultiplayer) return;

        var player = ctx.PlayerController.gameObject;
        _rpcController = player.GetComponent<RPCController>();

        // Server side spawn
        var playerNetworkBehaviour = player.GetComponent<NetworkBehaviour>();
        var localClientId = playerNetworkBehaviour.NetworkManager.LocalClientId;
        _rpcController.SpawnProjectileServerRpc(ctx.Direction, ctx.SpawnPos,
            ctx.TargetPos, localClientId, ability.key);
    }
    
    private GameObject SpawnAbilityPrefab(Ability ability, CastContext ctx) { 
        var newProjectile = ClientObjectPool.Instance.GetPooledObject(ability, AbilityPrefabType.Spawn);
        if (newProjectile == null) {
            Debug.Log("No available projectiles in the pool");
            return null;
        }
        
        // Store prefab in a dictionary in case we need to access it later
        ctx.PlayerController.ActiveAbilityPrefabs[ability.key] = newProjectile;

        // Set the position and rotation of the projectile
        newProjectile.transform.position = ctx.SpawnPos;
        newProjectile.transform.rotation = Quaternion.LookRotation(ctx.Direction, Vector3.up);

        // Activate the projectile
        newProjectile.SetActive(true);
        
        return newProjectile;
    }

    private void InitAbilityScript(Ability ability, CastContext ctx, GameObject abilityPrefab) {
        // Initialize projectile properties
        var abilityScript = abilityPrefab.GetComponent<ClientAbilityBehaviour>();
        if (abilityScript != null) {
            abilityScript.InitialiseProperties(ability, ctx.PlayerController,ctx.TargetPos, ctx.Direction);
            abilityScript.ResetVFX();
        }
        else Debug.Log("ProjectileAbility component is missing on the projectile");
    }
    
    public void Recast(GameObject activeObject, string key) {
        // Client side recast
        var abilityScript = activeObject.GetComponent<ClientAbilityBehaviour>();
        abilityScript.isRecast = true;
        
        if (!_isMultiplayer) return;

        // Recast ability on server
        _rpcController.RecastAbilityServerRpc(key);
    }

}
