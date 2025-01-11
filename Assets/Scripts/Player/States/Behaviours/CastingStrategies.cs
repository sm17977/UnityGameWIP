using Multiplayer;
using Unity.Netcode;
using UnityEngine;

public class SinglePlayerCastingStrategy : ICastingStrategy {

    private readonly LuxPlayerController _playerController;

    public SinglePlayerCastingStrategy(LuxPlayerController playerController) {
        _playerController = playerController;
    }
    
    public void Cast(Ability ability, Vector3 targetDir, Vector3 targetPos, Vector3 abilitySpawnPos) {
        GameObject newProjectile = Object.Instantiate(ability.missilePrefab, abilitySpawnPos, Quaternion.LookRotation(targetDir, Vector3.up));
        _playerController.projectiles.Add(newProjectile);
        ClientAbilityBehaviour projectileScript = newProjectile.GetComponent<ClientAbilityBehaviour>();
        projectileScript?.InitialiseProperties(ability, _playerController,targetPos, targetDir);
    }

    public void ReCast(GameObject projectile, string abilityKey) {
        throw new System.NotImplementedException();
    }
}

public class MultiplayerCastingStrategy : ICastingStrategy {
    
    private readonly RPCController _rpcController;
    private readonly LuxPlayerController _playerController;
    private readonly GameObject _player;
    
    public MultiplayerCastingStrategy(GameObject player, RPCController rpcController) {
        _player = player;
        _playerController = player.GetComponent<LuxPlayerController>();
        _rpcController = rpcController;
    }
    
    public void Cast(Ability ability, Vector3 targetDir, Vector3 targetPos, Vector3 abilitySpawnPos) {
        
        var newProjectile = ClientObjectPool.Instance.GetPooledObject(ability, AbilityPrefabType.Projectile);
        if (newProjectile == null) {
            Debug.Log("No available projectiles in the pool");
            return;
        }
        
        // Store prefab in dictionary in case we need to access it later
        _playerController.ActiveAbilityPrefabs[ability.key] = newProjectile;

        // Set the position and rotation of the projectile
        newProjectile.transform.position = abilitySpawnPos;
        newProjectile.transform.rotation = Quaternion.LookRotation(targetDir, Vector3.up);

        // Activate the projectile
        newProjectile.SetActive(true);
            
        // Initialize projectile properties
        var projectileScript = newProjectile.GetComponent<ClientAbilityBehaviour>();
        if (projectileScript != null) {
            projectileScript.InitialiseProperties(ability, _playerController,targetPos, targetDir);
            projectileScript.ResetVFX();
     
        }
        else {
            Debug.Log("ProjectileAbility component is missing on the projectile");
        }

        var playerNetworkBehaviour = _player.GetComponent<NetworkBehaviour>();
        var localClientId = playerNetworkBehaviour.NetworkManager.LocalClientId;
        
        _rpcController.SpawnProjectileServerRpc(targetDir, abilitySpawnPos, targetPos, localClientId, newProjectile.transform.GetInstanceID(), ability.key);
    }

    public void ReCast(GameObject projectile, string abilityKey) {
        
        // Client side 
        var projectileScript = projectile.GetComponent<ClientAbilityBehaviour>();
        projectileScript.isRecast = true;
        
        // Server
        var playerNetworkBehaviour = _player.GetComponent<NetworkBehaviour>();
        var localClientId = playerNetworkBehaviour.NetworkManager.LocalClientId;
        _rpcController.ReCastAbilityServerRpc(localClientId, abilityKey);
        
    }
}
