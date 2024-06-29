using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CooldownManager : MonoBehaviour {
    public static CooldownManager Instance;
    private List<Ability> _abilitiesOnCooldown = new();

    private void Awake() {
        if (Instance == null) {
            Instance = this;
        }
        else if (Instance != this) {
            Destroy(this);
        }
    }

    private void Update() {
        foreach (var ability in _abilitiesOnCooldown.ToList()) {
            ability.currentCooldown -= Time.deltaTime;

            if (ability.currentCooldown <= 0) {
                ability.currentCooldown = 0;
                _abilitiesOnCooldown.Remove(ability);
            }
        }
    }

    /// <summary>
    /// Start cooldown timer of ability
    /// </summary>
    /// <param name="ability">The ability to put on cooldown</param>
    public void StartCooldown(Ability ability) {
        if (!_abilitiesOnCooldown.Contains(ability)) {
            ability.currentCooldown = ability.maxCooldown;
            _abilitiesOnCooldown.Add(ability);
        }
    }
}