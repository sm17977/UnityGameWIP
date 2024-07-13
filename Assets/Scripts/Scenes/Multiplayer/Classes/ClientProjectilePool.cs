using System;
using System.Collections.Generic;
using UnityEngine;

public class ClientProjectilePool : MonoBehaviour {
    public static ClientProjectilePool Instance { get; private set; }

    public int poolSize;
    private List<GameObject> _projectilePool;
    public GameObject projectilePrefab;

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
            var tmp = Instantiate(projectilePrefab, transform);
            tmp.SetActive(false);
            tmp.name = tmp.transform.GetInstanceID().ToString();
            _projectilePool.Add(tmp);
            Debug.Log("Projectile initialized and added to pool: " + tmp.name);
        }
    }

    public GameObject GetPooledProjectile() {
        foreach (var projectile in _projectilePool) {
            try {
                if (projectile == null) {
                    Debug.LogError("Projectile in pool is null!");
                    continue;
                }
                
                if (!projectile.activeInHierarchy) {
                    Debug.Log("Getting projectile");
                    return projectile;
                }
            }
            catch (Exception e) {
                Debug.Log("Error getting pooled projectile: " + e);
            }
        }
        return null;
    }
    public void ReturnProjectileToPool(GameObject projectile) {
        if (_projectilePool.Contains(projectile)) {
            projectile.SetActive(false);
        
            Debug.Log("Projectile returned to pool: " + projectile.name);
        } 
        else {
            Debug.LogError("Projectile not part of the pool: " + projectile.name);
        }
    }
}
