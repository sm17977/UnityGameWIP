using System;
using System.Collections.Generic;
using UnityEngine;

namespace Multiplayer {
    public class ProjectilePool : MonoBehaviour {

        public int poolSize;
        private List<GameObject> _projectilePool;
        public GameObject projectilePrefab;
        

        private void Start() {
            _projectilePool = new List<GameObject>();
          
            for (int i = 0; i < poolSize; i++) {
                var tmp = Instantiate(projectilePrefab);
                tmp.SetActive(false);
                _projectilePool.Add(tmp);
            }
        }

        public GameObject getPooledProjectile() {
            foreach (var projectile in _projectilePool) {
                if (!projectile.activeInHierarchy) {
                    return projectile;
                }
            }
            return null;
        }
    }
}