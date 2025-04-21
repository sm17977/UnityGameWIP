using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class HealthBarManager {

    private VisualElement _template;
    private Dictionary<LuxPlayerController, Dictionary<string, VisualElement>> _playerHealthBarMappings;
    private const float HealthBarDefaultYOffset = -60f;
    private const float HealthBarDefaultWidth = 142f;

    public HealthBarManager(VisualElement template) {
        _template = template;
        _playerHealthBarMappings = new Dictionary<LuxPlayerController, Dictionary<string, VisualElement>>();
        Health.OnHealthChanged += UpdateHealthBar;
    }
    
    ~HealthBarManager() {
        Health.OnHealthChanged -= UpdateHealthBar;
    }

    /// <summary>
    /// Generate health bars for all players
    /// </summary>
    /// <param name="playerGameObjects"></param>
    public void GenerateHealthBars(List<GameObject> playerGameObjects) {
        foreach (var player in playerGameObjects) {
            var playerScript = player.GetComponent<LuxPlayerController>();
            if (_playerHealthBarMappings.ContainsKey(playerScript)) {
                continue; // Don't create duplicate health bars
            }

            var healthBarContainer = new VisualElement();
            var healthBar = new VisualElement();
            
            healthBarContainer.AddToClassList("health-bar-container");
            healthBar.AddToClassList("health-bar");
            healthBar.AddToClassList("health-bar-borders");
            healthBar.name = "health-bar";
            healthBar.style.width = new Length(HealthBarDefaultWidth, LengthUnit.Pixel);
            
            healthBarContainer.Add(healthBar);
            _template.Add(healthBarContainer);

            _playerHealthBarMappings[playerScript] = new();
            _playerHealthBarMappings[playerScript]["HealthBar"] = healthBarContainer;

            var playerNameContainer = new VisualElement();
            var playerNameLabel = new Label();
            
            playerNameContainer.AddToClassList("player-name-container");
            playerNameLabel.AddToClassList("player-name-label");

            playerNameLabel.name = "player-name-label";
            playerNameLabel.text = playerScript.playerName;
            playerNameContainer.Add(playerNameLabel);
            
            _template.Add(playerNameContainer);
            
            healthBarContainer.SendToBack();
            playerNameContainer.SendToBack();

            _playerHealthBarMappings[playerScript]["PlayerName"] = playerNameContainer;
        }
    }

    /// <summary>
    ///  Update the health bar for a specific player
    /// </summary>
    /// <param name="playerScript"></param>
    /// <param name="currentHealth"></param>
    /// <param name="maxHealth"></param>
    private void UpdateHealthBar(LuxPlayerController playerScript, float currentHealth, float maxHealth) {
        if(playerScript == null) return;
        if (_playerHealthBarMappings.ContainsKey(playerScript)) {
            var healthBarContainer = _playerHealthBarMappings[playerScript]["HealthBar"];
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

    public void UpdatePlayerNameLabels() {
        foreach (var player in _playerHealthBarMappings) {

            var playerScript = player.Key;
            
            if (_playerHealthBarMappings.ContainsKey(playerScript)) {
                var playerNameContainer = _playerHealthBarMappings[playerScript]["PlayerName"];
                var playerNameLabel = playerNameContainer.Q<Label>("player-name-label");
                playerNameLabel.text = playerScript.playerName;
            }
        }
    }
    
    /// <summary>
    /// Remove health bar when a player leaves
    /// </summary>
    /// <param name="playerScript"></param>
    public void RemoveHealthBar(LuxPlayerController playerScript) {
        if (_playerHealthBarMappings.ContainsKey(playerScript)) {
            _playerHealthBarMappings[playerScript]["HealthBar"].RemoveFromHierarchy();
            _playerHealthBarMappings[playerScript]["PlayerName"].RemoveFromHierarchy();
            _playerHealthBarMappings.Remove(playerScript);
        }
    }

    /// <summary>
    /// Update the position of the health bars above the player models
    /// </summary>
    public void SetHealthBarPosition(PanelSettings panelSettings) {
        foreach (var (player, elements) in _playerHealthBarMappings) {
            if (player == null || elements == null) continue;

            var healthBar = elements["HealthBar"];
            var playerName = elements["PlayerName"];

            Vector2 newPosition = RuntimePanelUtils.CameraTransformWorldToPanel(
                healthBar.panel, player.healthBarAnchor.transform.position, player.mainCamera);
            
            newPosition.x += -(panelSettings.referenceResolution.x / 2);
            newPosition.y += -(panelSettings.referenceResolution.y / 2) + HealthBarDefaultYOffset;
            
            healthBar.usageHints = UsageHints.DynamicTransform; 
            healthBar.style.translate = new StyleTranslate(new Translate(newPosition.x, newPosition.y, 0));
            playerName.style.translate = new StyleTranslate(new Translate(newPosition.x, newPosition.y + -30, 0));
            healthBar.MarkDirtyRepaint();
        }
    }
    
    /// <summary>
    /// Returns list of player scripts to remove when a player leaves the game
    /// </summary>
    /// <param name="currentPlayers"></param>
    /// <returns>List of player scripts to remove</returns>
    public List<LuxPlayerController> GetPlayerScriptsToRemove(List<GameObject> currentPlayers) {
        // List to hold player scripts whose health bars need to be removed
        var playerScriptsToRemove = new List<LuxPlayerController>();

        // Check for any players whose game object has been destroyed or are no longer in the current player list
        foreach (var playerScript in _playerHealthBarMappings.Keys) {
            // If the player's GameObject is null (destroyed) or not in the current list, add them to list to be removed
            if (playerScript == null || playerScript.gameObject == null || 
                !currentPlayers.Any(player => player.GetComponent<LuxPlayerController>() == playerScript)) {
                playerScriptsToRemove.Add(playerScript);
            }
        }
        return playerScriptsToRemove;
    }
}
