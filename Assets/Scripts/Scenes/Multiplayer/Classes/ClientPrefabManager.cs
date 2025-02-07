using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;

public class ClientPrefabManager : MonoBehaviour {
    public static ClientPrefabManager Instance { get; private set; }
    
    [SerializedDictionary("NetworkObjectId", "Prefab")]
    private static SerializedDictionary<ulong, GameObject> _localPrefabs;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
        }
    }

    private void Start() {
        #if !UNITY_SERVER
            _localPrefabs = new SerializedDictionary<ulong, GameObject>();
        #endif
    }

    public void RegisterPrefab(ulong networkObjectId, GameObject prefab) {
        _localPrefabs[networkObjectId] = prefab;
    }

    public GameObject GetLocalPrefab(ulong networkObjectId) {
        _localPrefabs.TryGetValue(networkObjectId, out var prefab);
        return prefab;
    }
    
    public void UnregisterPrefab(ulong networkObjectId) {
        if (_localPrefabs.ContainsKey(networkObjectId)) {
            _localPrefabs.Remove(networkObjectId);
        }
    }
}
