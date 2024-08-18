using System;
using System.Collections.Generic;
using UnityEngine;

public class ClientProjectilePool : MonoBehaviour {
    public static ClientProjectilePool Instance { get; private set; }

    public int poolSize;
    private List<GameObject> _projectilePool;
    public Ability ability;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
        }
    }

    private void Start() {
        #if !DEDICATED_SERVER
            InitializePool();
        #endif
    }

    private void InitializePool() {
        _projectilePool = new List<GameObject>();

        for (int i = 0; i < poolSize; i++) {
            var projectile = Instantiate(ability.missilePrefab, transform);
            projectile.SetActive(false);
            projectile.name = projectile.transform.GetInstanceID().ToString();
            
            var hit = Instantiate(ability.hitPrefab, transform);
            hit.SetActive(false);
            hit.name = hit.transform.GetInstanceID().ToString();
            
            _projectilePool.Add(projectile);
            _projectilePool.Add(hit);
        }
    }

    public GameObject GetPooledObject(ProjectileType type) {
        foreach (var projectile in _projectilePool) {
            try {
                if (projectile == null) {
                    Debug.LogError("Projectile in pool is null!");
                    continue;
                }
                
                if (!projectile.activeInHierarchy && projectile.gameObject.CompareTag(type.ToString())) {
                    return projectile;
                }
            }
            catch (Exception e) {
                Debug.LogError("Error getting pooled projectile: " + e);
            }
        }
        return null;
    }
    public void ReturnObjectToPool(GameObject projectile) {
        if (_projectilePool.Contains(projectile)) {
            projectile.SetActive(false);
        } 
        else {
            Debug.LogError("Projectile not part of the pool: " + projectile.name);
        }
    }
}
