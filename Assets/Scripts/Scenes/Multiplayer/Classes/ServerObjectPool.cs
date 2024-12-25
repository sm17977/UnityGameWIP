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

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
        }
    }

    private void Start() {
        #if DEDICATED_SERVER
            InitializePool();
        #endif
    }

    private void InitializePool() {
        _pool = new SerializedDictionary<Ability, SerializedDictionary<AbilityPrefabType, Queue<GameObject>>>();

        // Iterate over each unique ability
        foreach (var ability in abilityPool) {
            
            _pool[ability] = new SerializedDictionary<AbilityPrefabType, Queue<GameObject>>();
            _pool[ability][AbilityPrefabType.Projectile] = new Queue<GameObject>();
           

            // Pool size determines how many prefabs of each type we'll store
            for (var i = 0; i < poolSize; i++) {

                var projectile = Instantiate(ability.networkMissilePrefab, transform);
                projectile.SetActive(false);
                projectile.name = projectile.transform.GetInstanceID().ToString();
                projectile.GetComponent<Rigidbody>().isKinematic = false;

                _pool[ability][AbilityPrefabType.Projectile].Enqueue(projectile);
            }
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
