using UnityEngine;

public class Health : MonoBehaviour {
    public float maxHealth = 100f;
    private float _currentHealth;

    public delegate void HealthChanged(float currentHealth, float maxHealth);
    public event HealthChanged OnHealthChanged;

    public delegate void Death();
    public event Death OnDeath;

    private void Start() {
        _currentHealth = maxHealth;
        OnHealthChanged?.Invoke(_currentHealth, maxHealth);
    }

    public void TakeDamage(float damageAmount) {
        _currentHealth -= damageAmount;

        // Clamp health to prevent it from going negative
        _currentHealth = Mathf.Clamp(_currentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(_currentHealth, maxHealth);

        if (_currentHealth <= 0) {
            Die();
        }
    }

    public void Heal(float healAmount) {
        _currentHealth += healAmount;
        _currentHealth = Mathf.Clamp(_currentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(_currentHealth, maxHealth);
    }

    private void Die() {
        OnDeath?.Invoke();
    }

    public float GetCurrentHealth() {
        return _currentHealth;
    }

    public float GetMaxHealth() {
        return maxHealth;
    }
}