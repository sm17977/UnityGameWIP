using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class HealthBarManager {

    private VisualElement _template;
    private Dictionary<LuxPlayerController, VisualElement> _playerHealthBarMappings;
    private const float HealthBarYOffset = -50f;
    private const float HealthBarDefaultWidth = 150f;

    public HealthBarManager(VisualElement template) {
        _template = template;
        _playerHealthBarMappings = new Dictionary<LuxPlayerController, VisualElement>();
        Health.OnHealthChanged += UpdateHealthBar;
    }
    
    ~HealthBarManager() {
        Health.OnHealthChanged -= UpdateHealthBar;
    }

    // Generate health bars for all players
    public void GenerateHealthBars(List<GameObject> playerGameObjects) {
        foreach (var player in playerGameObjects) {
            var playerScript = player.GetComponent<LuxPlayerController>();
            if (_playerHealthBarMappings.ContainsKey(playerScript)) {
                continue; // Avoid creating duplicate health bars
            }

            var healthBarContainer = new VisualElement();
            var healthBar = new VisualElement();
            
            healthBarContainer.AddToClassList("health-bar-container");
            healthBar.AddToClassList("health-bar");
            healthBar.AddToClassList("health-bar-borders");
            healthBar.name = "health-bar";
            healthBar.style.width = new Length(150, LengthUnit.Pixel);
            
            healthBarContainer.Add(healthBar);
            _template.Add(healthBarContainer);
            
            _playerHealthBarMappings[playerScript] = healthBarContainer;
        }
    }

    // Update the health bar for a specific player
    public void UpdateHealthBar(LuxPlayerController playerScript, float currentHealth, float maxHealth) {
        if (_playerHealthBarMappings.ContainsKey(playerScript)) {
            var healthBarContainer = _playerHealthBarMappings[playerScript];
            var healthBar = healthBarContainer.Q<VisualElement>("health-bar");

            if (healthBar != null) {
                float healthPercentage = currentHealth / maxHealth;
                float containerWidth = HealthBarDefaultWidth;
                float newWidth = healthPercentage * containerWidth;

                if (currentHealth == 0) {
                    healthBar.RemoveFromClassList("health-bar-borders");
                }
                
                healthBar.style.width = new Length(newWidth, LengthUnit.Pixel);
            }
        }
    }
    
    // Remove health bar when a player leaves
    public void RemoveHealthBar(LuxPlayerController playerScript) {
        if (_playerHealthBarMappings.ContainsKey(playerScript)) {
            _playerHealthBarMappings[playerScript].RemoveFromHierarchy();
            _playerHealthBarMappings.Remove(playerScript);
        }
    }

    // Update the position of the health bars above the player models
    public void SetHealthBarPosition() {
        foreach (var (player, healthBar) in _playerHealthBarMappings) {
            if (player == null || healthBar == null) continue;

            Vector2 newPosition = RuntimePanelUtils.CameraTransformWorldToPanel(
                healthBar.panel, player.healthBarAnchor.transform.position, player.mainCamera);
            
            newPosition.x += -(Screen.width / 2);
            newPosition.y += -(Screen.height / 2) + HealthBarYOffset;

            healthBar.transform.position = newPosition;
        }
    }
    
    public List<LuxPlayerController> GetPlayerScriptsToRemove(List<GameObject> currentPlayers) {
        // List to hold player scripts whose health bars need to be removed
        var playerScriptsToRemove = new List<LuxPlayerController>();

        // Check for any players whose game object has been destroyed or are no longer in the current player list
        foreach (var playerScript in _playerHealthBarMappings.Keys) {
            // If the player's GameObject is null (destroyed) or not in the current list, mark for removal
            if (playerScript == null || playerScript.gameObject == null || 
                !currentPlayers.Any(player => player.GetComponent<LuxPlayerController>() == playerScript)) {
                playerScriptsToRemove.Add(playerScript);
            }
        }

        return playerScriptsToRemove;
    }

}
