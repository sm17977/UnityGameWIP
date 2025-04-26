using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class Health : NetworkBehaviour {
 
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>();
    private LuxPlayerController _playerScript;
    public float maxHealth;
    public delegate void HealthChanged(LuxPlayerController player, float currentHealth, float maxHealth, float damage);
    public delegate Task PlayerDied(LuxPlayerController player);
    public static event HealthChanged OnHealthChanged;
    public static event PlayerDied OnPlayerDeath;

    private void Start() {
        _playerScript = GetComponent<LuxPlayerController>();
        maxHealth = _playerScript.champion.health;
        if (IsServer) {
            currentHealth.Value = maxHealth;  
        }
    }
    
    public void TakeDamage(float amount) {
        if (IsServer) {
            currentHealth.Value -= amount;
            if (currentHealth.Value < 0) currentHealth.Value = 0;
        }
    }
    
    public void Heal(float amount) {
        if (IsServer) {
            currentHealth.Value += amount;
            if (currentHealth.Value > maxHealth) currentHealth.Value = maxHealth;
        }
    }

    public override void OnNetworkSpawn() {
        // Sync health across clients when it changes
        currentHealth.OnValueChanged += (oldHealth, newHealth) => {
            float diff = oldHealth - newHealth;
            OnHealthChanged?.Invoke(_playerScript, newHealth, maxHealth, diff);
            if(newHealth == 0) OnPlayerDeath?.Invoke(_playerScript);
        };
    }
}