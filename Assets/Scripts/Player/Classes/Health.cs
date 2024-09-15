using Unity.Netcode;
using UnityEngine;

public class Health : NetworkBehaviour {
 
    public NetworkVariable<float> currentHealth = new NetworkVariable<float>();
    public float maxHealth = 100f;
    private LuxPlayerController _playerScript;

    public delegate void HealthChanged(LuxPlayerController player, float currentHealth, float maxHealth);
    public static event HealthChanged OnHealthChanged;

    private void Start() {
        _playerScript = GetComponent<LuxPlayerController>();
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
            OnHealthChanged?.Invoke(_playerScript, newHealth, maxHealth);
        };
    }
}