using System;
using System.Collections.Generic;
using QFSW.QC;
using Unity.Netcode;
using UnityEngine;

namespace Multiplayer {
    public class ProjectilePool : MonoBehaviour {
        public static ProjectilePool Instance { get; private set; }

        public int poolSize;
        private List<GameObject> _projectilePool;
        public GameObject projectilePrefab;

        private void Awake() {
#if DEDICATED_SERVER            
            if (Instance == null) {
                Instance = this;
            }
            else {
                Destroy(gameObject);
            }
#endif
        }

        private void Start() {
#if DEDICATED_SERVER            
            InitializePool();
#endif
        }

        private void InitializePool() {
            _projectilePool = new List<GameObject>();

            for (int i = 0; i < poolSize; i++) {
                var tmp = Instantiate(projectilePrefab);
                tmp.SetActive(false);
                _projectilePool.Add(tmp);
            }
        }

        public GameObject GetPooledProjectile() {
            foreach (var projectile in _projectilePool) {
                try {
                    if (projectile == null) {
                        Debug.LogError("Projectile in pool is null!");
                        continue;
                    }

                    var networkObject = projectile.GetComponent<NetworkObject>();
                    if (networkObject == null) {
                        Debug.LogError("NetworkObject component is missing on projectile: " + projectile.name);
                        continue;
                    }
                    if (!projectile.activeInHierarchy && !networkObject.IsSpawned) {
                        return projectile;
                    }
                }
                catch (Exception e) {
                    Debug.Log("Error getting pooled projectile: " + e);
                }
            }
            return null;
        }
    }
}