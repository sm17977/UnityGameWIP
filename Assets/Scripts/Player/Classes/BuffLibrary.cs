using System.Collections.Generic;
using UnityEngine;

public class BuffLibrary : MonoBehaviour {

    public static BuffLibrary Instance { get; private set; }
    
    // Dictionary to store buffs by name for quick access
    private Dictionary<string, Buff> _buffDictionary = new Dictionary<string, Buff>();

    [Header("All Buffs in the Game")]
    [SerializeField] private List<Buff> buffs;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeBuffDictionary();
        } else {
            Destroy(gameObject);
        }
    }

    private void InitializeBuffDictionary() {
        foreach (var buff in buffs) {
            _buffDictionary[buff.name] = buff;
        }
    }

    // Public method to retrieve a buff by name
    public Buff GetBuffByName(string buffName) {
        if (_buffDictionary.TryGetValue(buffName, out var buff)) {
            return buff;
        }
        Debug.LogWarning($"Buff with name {buffName} not found.");
        return null;
    }
}