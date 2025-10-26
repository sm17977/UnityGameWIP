using System.Collections.Generic;
using System.Linq;
using Scenes.Multiplayer.UI.Game_View_Classes;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class FloatingUIManager {

    private VisualElement _template;
    private Dictionary<LuxPlayerController, Dictionary<UIComponent, VisualElement>> _playerUIComponents;
    private const float HealthBarDefaultYOffset = -60f;
    private const float HealthBarDefaultWidth = 142f;
    private const int HealthBarSections = 10;

    public FloatingUIManager(VisualElement template) {
        _template = template;
        _playerUIComponents = new Dictionary<LuxPlayerController, Dictionary<UIComponent, VisualElement>>();
        Health.OnHealthChanged += UpdateHealthBar;
    }
    
    ~FloatingUIManager() {
        Health.OnHealthChanged -= UpdateHealthBar;
    }

    /// <summary>
    /// Generate the UI components for each player
    /// </summary>
    /// <param name="playerGameObjects"></param>
    public void GenerateUIComponents(List<GameObject> playerGameObjects) {
        foreach (var player in playerGameObjects) {
            var playerScript = player.GetComponent<LuxPlayerController>();
            if (_playerUIComponents.ContainsKey(playerScript)) {
                continue; // Don't create duplicate UI components
            }
            
            var healthBarContainer = CreateHealthBarContainer();
            var playerNameContainer = CreatePlayerNameContainer(playerScript);
            var playerDamageTextContainer = CreatePlayerDamageTextContainer(playerScript);
            
            _playerUIComponents[playerScript] = new() {
                [UIComponent.HealthBar]        = healthBarContainer,
                [UIComponent.PlayerName]       = playerNameContainer,
                [UIComponent.PlayerDamageText] = playerDamageTextContainer
            };
            
            new List<VisualElement> {
                healthBarContainer,
                playerNameContainer,
                playerDamageTextContainer
            }.ForEach(element => {
                _template.Add(element);
                element.SendToBack();
            });
        }
    }
    
    /// <summary>
    /// Create the health bar container
    /// </summary>
    /// <returns></returns>
    private VisualElement CreateHealthBarContainer() {
        var healthBarContainer = new VisualElement();
        var healthBar = new VisualElement();
            
        healthBarContainer.AddToClassList("health-bar-container");
        healthBar.AddToClassList("health-bar");
        healthBar.AddToClassList("health-bar-borders");
        healthBar.name = "health-bar";
        healthBar.style.width = new Length(HealthBarDefaultWidth, LengthUnit.Pixel);
        healthBarContainer.Add(healthBar);
        
        for(var i = 0; i < HealthBarSections; i++) {
            var section = new VisualElement();
            section.AddToClassList("health-bar-section");
            if(i < HealthBarSections - 1) {
                section.AddToClassList("border-right");
            }
            healthBar.Add(section);
        }

        return healthBarContainer;
    }

    /// <summary>
    /// Create the player name label container
    /// </summary>
    /// <param name="playerScript"></param>
    /// <returns></returns>
    private VisualElement CreatePlayerNameContainer(LuxPlayerController playerScript) {
        var playerNameContainer = new VisualElement();
        var playerNameLabel = new Label();
            
        playerNameContainer.AddToClassList("player-name-container");
        playerNameLabel.AddToClassList("player-name-label");

        playerNameLabel.name = "player-name-label";
        playerNameLabel.text = playerScript.playerName;
        playerNameContainer.Add(playerNameLabel);

        return playerNameContainer;
    }
    
    /// <summary>
    /// Create the player damage text element container
    /// </summary>
    /// <param name="playerScript"></param>
    /// <returns></returns>
    private VisualElement CreatePlayerDamageTextContainer(LuxPlayerController playerScript) {
        var playerDamageTextContainer = new VisualElement();
        var playerDamageTextLabel = new Label();
            
        playerDamageTextContainer.AddToClassList("damage-number-container");
        playerDamageTextLabel.AddToClassList("damage-number-label");

        playerDamageTextLabel.name = "damage-number-label";
        playerDamageTextLabel.text = "56";
        playerDamageTextContainer.Add(playerDamageTextLabel);

        return playerDamageTextContainer;
    }

    /// <summary>
    ///  Update the health bar for a specific player
    /// </summary>
    /// <param name="playerScript"></param>
    /// <param name="currentHealth"></param>
    /// <param name="maxHealth"></param>
    /// <param name="damage"></param>
    private void UpdateHealthBar(LuxPlayerController playerScript, float currentHealth, float maxHealth, float damage) {
        if(playerScript == null) return;
        if (_playerUIComponents.ContainsKey(playerScript)) {
            var healthBarContainer = _playerUIComponents[playerScript][UIComponent.HealthBar];
            var healthBar = healthBarContainer.Q<VisualElement>("health-bar");

            var damageTextLabelContainer = _playerUIComponents[playerScript][UIComponent.PlayerDamageText];
            var damageTextLabel = damageTextLabelContainer.Q<Label>("damage-number-label");
            ShowDamageNumberLabel(damageTextLabel, damage);

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

    private void ShowDamageNumberLabel(Label label, float damageAmount) {
        
        label.text = damageAmount.ToString();
        label.AddToClassList("damage-number-label-active");
        
        label.schedule
            .Execute(() => {
                label.RemoveFromClassList("damage-number-label-active");
            })
            .StartingIn(1000);    
    }
    

    /// <summary>
    /// Update the player name labels for all players
    /// </summary>
    public void UpdatePlayerNameLabels() {
        foreach (var player in _playerUIComponents) {
            if (_playerUIComponents.ContainsKey(player.Key)) {
                var playerNameContainer = _playerUIComponents[player.Key][UIComponent.PlayerName];
                var playerNameLabel = playerNameContainer.Q<Label>("player-name-label");
                playerNameLabel.text = player.Key.playerName;
            }
        }
    }
    
    /// <summary>
    /// Remove the player script and all the UI components from the dictionary whenever a player leaves
    /// </summary>
    /// <param name="playerScript"></param>
    public void RemoveUIComponents(LuxPlayerController playerScript) {
        if (_playerUIComponents.ContainsKey(playerScript)) {
            foreach (var kvp in _playerUIComponents[playerScript]) {
                kvp.Value.RemoveFromHierarchy();
            }
            _playerUIComponents.Remove(playerScript);
        }
    }

    /// <summary>
    /// Update the position of each UI component based on the player's position
    /// </summary>
    public void SetUIComponentPositions(PanelSettings panelSettings) {
        foreach (var (player, elements) in _playerUIComponents) {
            if (player == null || elements == null) continue;

            var healthBar = elements[UIComponent.HealthBar];
            var playerName = elements[UIComponent.PlayerName];
            var playerDamageText = elements[UIComponent.PlayerDamageText];

            Vector2 newPosition = RuntimePanelUtils.CameraTransformWorldToPanel(
                healthBar.panel, player.healthBarAnchor.transform.position, player.mainCamera);
            
            newPosition.x += -(panelSettings.referenceResolution.x / 2);
            newPosition.y += -(panelSettings.referenceResolution.y / 2) + HealthBarDefaultYOffset;
            
            //healthBar.usageHints = UsageHints.DynamicTransform; 
            
            healthBar.style.translate = new StyleTranslate(new Translate(newPosition.x, newPosition.y, 0));
            playerName.style.translate = new StyleTranslate(new Translate(newPosition.x, newPosition.y + -30, 0));
            playerDamageText.style.translate = new StyleTranslate(new Translate(newPosition.x, newPosition.y + 100, 0));
            
            //healthBar.MarkDirtyRepaint();
        }
    }
    
    /// <summary>
    /// Returns list of player scripts to remove when a player leaves the game
    /// </summary>
    /// <param name="currentPlayers"></param>
    /// <returns>List of player scripts to remove</returns>
    public List<LuxPlayerController> GetPlayerScriptsToRemove(List<GameObject> currentPlayers) {
        var playerScriptsToRemove = new List<LuxPlayerController>();
        
        _playerUIComponents.Keys.ToList().ForEach(playerScript => {
            if (playerScript == null || playerScript.gameObject == null
                || !currentPlayers.Any(player => player.GetComponent<LuxPlayerController>() == playerScript))
                playerScriptsToRemove.Add(playerScript);
        });
        return playerScriptsToRemove;
    }
}
