using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class ChampionRegistry : MonoBehaviour {
    public static ChampionRegistry Instance { get; private set; }
    
    // Dictionary to store buffs by name for quick access
    public List<Champion> championRegistryList = new();
    
    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }
    

    // Public method to retrieve a buff by name
    public Champion Get(string champName) {
        return championRegistryList.Find(champion => champion.name == champName);
    }
    
    public Dictionary<string, Ability> GetAbilities(string champName) {
        var champion = championRegistryList.Find(champion => champion.name == champName);
        return new Dictionary<string, Ability>() {
            { "Q", champion.abilityQ },
            { "W", champion.abilityW },
            { "E", champion.abilityE },
            { "R", champion.abilityR },
        };
    }
}