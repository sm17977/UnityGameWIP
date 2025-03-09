using System;
using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using Unity.Netcode;
using UnityEngine;

public class ServerObjectPool : MonoBehaviour {
    public static ServerObjectPool Instance { get; private set; }

    public int poolSize;
    public SerializedDictionary<Ability, SerializedDictionary<AbilityPrefabType, Queue<GameObject>>> _pool;

    [SerializeField]
    public List<Ability> abilityPool;
    
    public GameObject autoAttackPrefab;
    private List<GameObject> _autoAttackPool;
    private int _autoAttackPoolSize = 15;


    private void Awake() {
        if (Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
        }
    }

    private void Start() {
        #if UNITY_SERVER
            InitializePool();
        #endif
    }

    private void InitializePool() {

        Debug.Log("Initializing Server Object Pool");
        
        _pool = new SerializedDictionary<Ability, SerializedDictionary<AbilityPrefabType, Queue<GameObject>>>();
        _autoAttackPool = new List<GameObject>();
        
        // Iterate over each unique ability
        foreach (var ability in abilityPool) {
            
            _pool[ability] = new SerializedDictionary<AbilityPrefabType, Queue<GameObject>>();
            _pool[ability][AbilityPrefabType.Projectile] = new Queue<GameObject>();
            
            // Pool size determines how many prefabs of each type we'll store
            for (var i = 0; i < poolSize; i++) {

                Debug.Log("Instantiating ability " + ability.key);

                var projectile = Instantiate(ability.networkMissilePrefab, transform);
                projectile.SetActive(false);
                projectile.name = projectile.transform.GetInstanceID().ToString();
                projectile.GetComponent<Rigidbody>().isKinematic = false;

                _pool[ability][AbilityPrefabType.Projectile].Enqueue(projectile);
            }
        }
        InitAAPrefabs();
    }
    
    private void InitAAPrefabs() {
        for (int i = 0; i < _autoAttackPoolSize; i++) {
            var autoAttack = Instantiate(autoAttackPrefab, transform);
            autoAttack.name = autoAttack.transform.GetInstanceID().ToString();
            autoAttack.SetActive(false);
            _autoAttackPool.Add(autoAttack);
        }
    }

    public GameObject GetPooledObject(Ability ability, AbilityPrefabType prefabType) {
        
        var matchingAbility = FindAbilityByName(ability.abilityName);
        if (matchingAbility == null) {
            Debug.LogError($"No ability in the pool with name '{ability.abilityName}'.");
            return null;
        }
        
        if (!_pool.TryGetValue(matchingAbility, out SerializedDictionary<AbilityPrefabType, Queue<GameObject>> test)) {
            Debug.LogError($"No pool for ability: {matchingAbility.name}");
            return null;
        }

        if (!_pool[matchingAbility].ContainsKey(prefabType)) {
            Debug.LogError($"Ability '{matchingAbility.name}' has no pool for prefab type '{prefabType}'.");
            return null;
        }

        var queue = _pool[matchingAbility][prefabType];
        if (queue.Count == 0) {
            Debug.LogWarning($"Pool is empty for ability '{matchingAbility.name}' and prefab type '{prefabType}'. " +
                             $"Consider increasing poolSize or dynamically instantiate more.");
            return null;
        }
        
        var obj = queue.Dequeue();
        if (obj.activeInHierarchy) return null;
        return obj;
    }
    
    public GameObject GetPooledAutoAttack() {
        if (_autoAttackPool.Count > 0) {
            var autoAttack = _autoAttackPool[0];
            _autoAttackPool.RemoveAt(0);
            
            var networkObject = autoAttack.GetComponent<NetworkObject>();
            if (networkObject != null) {
                networkObject.Spawn();
                Debug.Log("Spawned AA");
            }
            else {
                Debug.Log("Failed to spawn AA");
            }
        
            return autoAttack;
        }

        return null;
    }
    
    public void ReturnPooledAutoAttack(GameObject autoAttack) {
        Debug.Log("Returning AA to pool");
        if (autoAttack == null) return;
        
        var networkObject = autoAttack.GetComponent<NetworkObject>();
        if (networkObject != null && networkObject.IsSpawned) {
            networkObject.Despawn(false);
        }

        autoAttack.SetActive(false);
        _autoAttackPool.Add(autoAttack);
    }
    
    public void ReturnObjectToPool(Ability ability, AbilityPrefabType prefabType, GameObject obj) {
        if (obj == null) return;
        obj.SetActive(false);
        
        var matchingAbility = FindAbilityByName(ability.abilityName);
        if(matchingAbility == null) return;
        
        if (!_pool[matchingAbility].ContainsKey(prefabType)) {
            Debug.LogError($"Trying to return object to a pool that doesn't exist: " +
                           $"ability '{ability.name}' type '{prefabType}'");
            return;
        }

        if (prefabType != AbilityPrefabType.Hit) {
            var networkObject = obj.GetComponent<NetworkObject>();
            if (networkObject != null && networkObject.IsSpawned) {
                networkObject.Despawn(false);
            }
        }
        _pool[matchingAbility][prefabType].Enqueue(obj);
    }
    
    private Ability FindAbilityByName(string abilityName) {
        foreach (var kvp in _pool) {
            Ability keyAbility = kvp.Key;
            if (keyAbility.abilityName == abilityName) {
                return keyAbility;
            }
        }
        return null; 
    }
}
