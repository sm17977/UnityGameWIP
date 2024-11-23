using System;
using Unity.Netcode;
using UnityEngine;

public class RPCController : NetworkBehaviour {

    public event Action<bool> OnCooldownReceived;
    public static event Action<GameObject> NetworkSpawn;
    
    private LuxPlayerController _playerController;
    private GameObject _player;
    private GameObject _players;
    
    private void Start() {
        _playerController = GetComponent<LuxPlayerController>();
        _player = gameObject;
    }

    public override void OnNetworkSpawn() {
      
        Debug.Log("RPC OnNetWorkSpawn");
        gameObject.name = IsLocalPlayer ? "Local Player" : GetComponent<NetworkObject>().NetworkObjectId.ToString();
        _players = GameObject.Find("Players");
        

        if (IsServer) {
            transform.SetParent(_players.transform, true);
        }

        if (IsOwner) {
            NetworkSpawn?.Invoke(gameObject);
        }
    }

    [Rpc(SendTo.Server)]
    public void SpawnProjectileServerRpc(Vector3 direction, Vector3 position, ulong clientId, int instanceId, string abilityKey) {
        
        var newNetworkProjectile = ServerProjectilePool.Instance.GetPooledProjectile();
        if (newNetworkProjectile == null) {
            Debug.Log("No available projectiles in the pool");
            return;
        }

        var networkInstanceId = newNetworkProjectile.transform.GetInstanceID();
        
        // Set the position and rotation of the projectile
        newNetworkProjectile.transform.position = position;
        newNetworkProjectile.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        // Activate the projectile
        newNetworkProjectile.SetActive(true);
        
        var networkObject = newNetworkProjectile.GetComponent<NetworkObject>();
        if (!networkObject.IsSpawned) {
            networkObject.Spawn(true);
        }
        
        // Initialize projectile properties on the server
        var projectileScript = newNetworkProjectile.GetComponent<NetworkProjectileAbility>();
        if (projectileScript != null) {
            Debug.Log("Init Projectile Server Ability - " + _playerController.Abilities[abilityKey].buff.ID);
            projectileScript.InitProjectileProperties(direction, _playerController.Abilities[abilityKey], _playerController.playerType, clientId);
            projectileScript.Mappings[instanceId] = clientId;
        }
        else {
            Debug.Log("Projectile Script is null");
        }
        SpawnProjectileClientRpc(direction, position, networkInstanceId, abilityKey);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SpawnProjectileClientRpc(Vector3 direction, Vector3 position, int networkInstanceId, string abilityKey) {
        if (!IsOwner && !IsServer) {
        
            var newProjectile = ClientProjectilePool.Instance.GetPooledObject(ProjectileType.Projectile);
            if (newProjectile == null) {
                Debug.Log("No available projectiles in the pool");
            }

            // Set the position and rotation of the projectile
            newProjectile.transform.position = position;
            newProjectile.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            // Activate the projectile
            newProjectile.SetActive(true);
            
            // Initialize projectile properties on the server
            var projectileScript = newProjectile.GetComponent<ProjectileAbility>();
            if (projectileScript != null) {
                projectileScript.InitProjectileProperties(direction, _playerController.Abilities[abilityKey],
                    _playerController.projectiles, _playerController.playerType);
                projectileScript.ResetVFX();
            }
            else {
                Debug.Log("ProjectileAbility component is missing on the projectile");
            }
            
            var playerNetworkBehaviour = _player.GetComponent<NetworkBehaviour>();
            var localClientId = playerNetworkBehaviour.NetworkManager.LocalClientId;
            var localInstanceId = newProjectile.transform.GetInstanceID();
            
            UpdateMappingsServerRpc(localClientId, localInstanceId, networkInstanceId);
        }
    }

    [Rpc(SendTo.Server)]
    private void UpdateMappingsServerRpc(ulong clientId, int localProjectileId, int networkInstanceId) {
        var networkProjectile = GameObject.Find(networkInstanceId.ToString());
        var networkProjectileScript = networkProjectile.GetComponent<Lux_Q_Mis_Net>();
        networkProjectileScript.Mappings[localProjectileId] = clientId;
    }

    [Rpc(SendTo.Server)]
    public void AddCooldownRpc(ulong clientId, NetworkAbilityData abilityData) {
        NetworkCooldownManager.Instance.StartCooldown(clientId, abilityData);
    }

    [Rpc(SendTo.Server)]
    public void IsAbilityOnCooldownRpc(ulong clientId, NetworkAbilityData abilityData) {
        var cd = NetworkCooldownManager.Instance.IsAbilityOnCooldown(clientId, abilityData);
        UpdateCooldownRpc(cd);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateCooldownRpc(bool serverCooldown) {
        if (IsLocalPlayer && IsOwner) {
            OnCooldownReceived?.Invoke(serverCooldown);
            OnCooldownReceived = null;
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void UpdateBuffRpc(ulong sourceClientId, ulong targetClientId, string buffId, bool apply) {

        if (!IsOwner) return;

        Debug.Log("sourceClientId: " + sourceClientId);
        Debug.Log("targetClientId: " + targetClientId);
        Debug.Log("buffId: " + buffId);
        Debug.Log("Players Length: " + _players.transform.childCount);
        
        // Find the attacker player to retrieve the correct Buff
        Buff buffToApply = null;
        
        foreach (Transform child in _players.transform) {
            var player = child.gameObject;
            var networkObject = player.GetComponent<NetworkObject>();
            var clientId = networkObject.OwnerClientId;
            
            Debug.Log("Players loop, ClientId: " + clientId);

            if (clientId == sourceClientId) {
                Debug.Log("Find Buff Loop, found attacker");
                var attackerPlayerScript = player.GetComponent<LuxPlayerController>();
                var abilities = attackerPlayerScript.Abilities;

                Debug.Log("Abilities count: " + attackerPlayerScript.Abilities.Count);
        
                foreach (var ability in abilities.Values) {
                    Debug.Log("Find Buff Loop, buff ID: " + ability?.buff.ID);
                    if (ability?.buff != null && ability.buff.ID == buffId) {
                        buffToApply = ability.buff;
                        break;
                    }
                    if(ability == null){
                        Debug.Log("Ability is null!");
                    }
                }
                break;  // Exit loop once attacker is found
            }
        }

        if (buffToApply == null) {
            Debug.LogError("Buff not found on attacker’s abilities.");
            return;
        }

        // Find the target player and apply the Buff
        foreach (Transform child in _players.transform) {
            var player = child.gameObject;
            var networkObject = player.GetComponent<NetworkObject>();
            var clientId = networkObject.OwnerClientId;

            if (clientId == targetClientId) {
                var targetPlayerScript = player.GetComponent<LuxController>();

                // Apply or remove the Buff effect on the target
                if (apply) {
                    buffToApply.Effect.ApplyEffect(targetPlayerScript, buffToApply.EffectStrength);
                } else {
                    buffToApply.Effect.RemoveEffect(targetPlayerScript, buffToApply.EffectStrength);
                }
                break;
            }
        }
    }
}
