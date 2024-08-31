using System.Collections.Generic;
using System.Threading.Tasks;
using Multiplayer.UI;
using UnityEngine;
using UnityEngine.UIElements;

public delegate Task OnStartGame();
public delegate Task OnStartServer();
public delegate bool OnCanStartGame();
public delegate bool OnCanStartServer();

public class LobbyHostView : LobbyView {
    
    public event OnStartGame StartGame;
    public event OnStartServer StartServer;
    public event OnCanStartGame CanStartGame;
    public event OnCanStartServer CanStartServer;

    private Label _lobbyStatus;
    private Button _startServerBtn;
    private VisualElement _startServerBtnContainer;
    private Button _startGameBtn;
    private VisualElement _startGameBtnContainer;

    public enum HostLobbyStatus {
        WaitForPlayers,
        WaitForPlayersReady,
        WaitForServer,
        StartServer,
        StartGame
    };
    
    private static readonly Dictionary<HostLobbyStatus, string> HostStatuses = new() {
        {HostLobbyStatus.WaitForPlayers, "Waiting for players to join..."},
        {HostLobbyStatus.WaitForPlayersReady, "Waiting for players to ready up..."},
        {HostLobbyStatus.WaitForServer, "Setting things up..."},
        {HostLobbyStatus.StartServer, "Start the server"},
        {HostLobbyStatus.StartGame, "Ready to start the game"},
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
        
        _startServerBtn = Template.Q<Button>("start-server-btn");
        _startServerBtnContainer = Template.Q<VisualElement>("start-server-btn-container");
        
        _startGameBtn = Template.Q<Button>("start-game-btn");
        _startGameBtnContainer = Template.Q<VisualElement>("start-game-btn-container");
        
        _startGameBtn.RegisterCallback<ClickEvent>(evt => OnStartGameBtnClick());
        _startServerBtn.RegisterCallback<ClickEvent>(evt => OnStartServerBtnClick());
        
        SetStatus(HostLobbyStatus.StartServer);
    }

    /// <summary>
    /// Reset buttons when host has left a lobby
    /// </summary>
    public void Reset() {
        Hide(_startGameBtnContainer);
        Show(_startServerBtnContainer);
        _startServerBtn.SetEnabled(true);
        SetStatus(HostLobbyStatus.StartServer);
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
    /// Handle the start server button
    /// Disables and hides the button and triggers the start server event
    /// </summary>
    private async void OnStartServerBtnClick() {
        SetStatus(HostLobbyStatus.WaitForServer);
        _startServerBtn.SetEnabled(false);
        await StartServer?.Invoke();
        Hide(_startServerBtnContainer);
        _startGameBtn.SetEnabled(false);
        Show(_startGameBtnContainer);
        RePaint();
    }
    
    /// <summary>
    /// Overrides the LobbyView update function
    /// Calls the LobbyView update then triggers events to determine if
    /// any buttons should be disabled or enabled
    /// </summary>
    public override async void RePaint() {
        Debug.Log("Lobby Host calling repaint");
        _startServerBtn.SetEnabled(CanStartServer?.Invoke() == true);
        _startGameBtn.SetEnabled(CanStartGame?.Invoke() == true);
        base.RePaint();
    }
}
