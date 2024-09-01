using System.Collections.Generic;
using System.Threading.Tasks;
using Multiplayer.UI;
using UnityEngine;
using UnityEngine.UIElements;

public delegate Task OnStartGame();
public delegate bool OnCanStartGame();
public delegate bool OnCanStartServer();

public class LobbyHostView : LobbyView {
    
    public event OnStartGame StartGame;
    public event OnCanStartGame CanStartGame;
    public event OnCanStartServer CanStartServer;

    private Label _lobbyStatus;
    private Button _startGameBtn;
    private VisualElement _startGameBtnContainer;

    public enum HostLobbyStatus {
        WaitForServer,
        WaitForPlayers,
        WaitForPlayersReady,
        StartGame
    };
    
    private static readonly Dictionary<HostLobbyStatus, string> HostStatuses = new() {
        {HostLobbyStatus.WaitForServer, "Setting things up..."},
        {HostLobbyStatus.WaitForPlayers, "Waiting for players to join..."},
        {HostLobbyStatus.WaitForPlayersReady, "Waiting for players to ready up..."},
        {HostLobbyStatus.StartGame, "Ready to start the game"}
    };
    
    public LobbyHostView(VisualElement parentContainer, VisualTreeAsset vta): base(parentContainer, vta) {
        BindUIElements();
    }

    /// <summary>
    /// Initialize the start game and start server buttons
    /// Registers the button click events
    /// </summary>
    private void BindUIElements() {
        _lobbyStatus = Template.Q<Label>("lobby-status");
        
        _startGameBtn = Template.Q<Button>("start-game-btn");
        _startGameBtnContainer = Template.Q<VisualElement>("start-game-btn-container");
        _startGameBtn.RegisterCallback<ClickEvent>(evt => OnStartGameBtnClick());
        
        SetStatus(HostLobbyStatus.WaitForServer);
    }

    /// <summary>
    /// Reset buttons when host has left a lobby
    /// </summary>
    public void Reset() {
        SetStatus(HostLobbyStatus.WaitForServer);
    }

    /// <summary>
    /// Set the lobby status text
    /// </summary>
    /// <param name="status"></param>
    public void SetStatus(HostLobbyStatus status) {
        if(HostStatuses.TryGetValue(status, out string newStatus)) {
            _lobbyStatus.text = newStatus;
        }
    }
    
    /// <summary>
    /// Handle the start game button
    /// Disables the button and triggers the start game event
    /// </summary>
    private async void OnStartGameBtnClick() {
        _startGameBtn.SetEnabled(false);
        await StartGame?.Invoke();
    }
    
    /// <summary>
    /// Overrides the LobbyView update function
    /// Calls the LobbyView update then triggers events to determine if
    /// any buttons should be disabled or enabled
    /// </summary>
    public override async void RePaint() {
        Debug.Log("Lobby Host calling repaint");
        _startGameBtn.SetEnabled(CanStartGame?.Invoke() == true);
        base.RePaint();
    }
}
