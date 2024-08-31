
using System.Collections.Generic;
using System.Threading.Tasks;
using Multiplayer.UI;
using UnityEngine.UIElements;

public delegate Task OnReadyUp();

public class LobbyPlayerView : LobbyView {

    public event OnReadyUp ReadyUp;
    
    private Label _lobbyStatus;
    private Button _readyBtn;
    private PlayerLobbyStatus _currentStatus;
    
    public enum PlayerLobbyStatus {
        ReadyUp,
        WaitingForHost
    };
    
    private static readonly Dictionary<PlayerLobbyStatus, string> PlayerStatuses = new() {
        {PlayerLobbyStatus.ReadyUp, "Ready up!"},
        {PlayerLobbyStatus.WaitingForHost, "Waiting for host to start the game..."},
    };
    
    public LobbyPlayerView(VisualElement parentContainer, VisualTreeAsset vta): base(parentContainer, vta) {
        BindUIElements();
    }
    
    /// <summary>
    /// Initialize the ready up button
    /// Registers button's click event
    /// </summary>
    private void BindUIElements() {
        _readyBtn = Template.Q<Button>("ready-btn");
        _readyBtn.RegisterCallback<ClickEvent>((evt) => OnReadyUpBtnClick());

        _lobbyStatus = Template.Q<Label>("lobby-status");
        
        _currentStatus = PlayerLobbyStatus.ReadyUp;
        SetStatus(_currentStatus);
    }

    /// <summary>
    /// Reset lobby status
    /// </summary>
    public void Reset() {
        _currentStatus = PlayerLobbyStatus.ReadyUp;
        SetStatus(_currentStatus);
    }
    
    /// <summary>
    /// Set the lobby status text
    /// </summary>
    /// <param name="status"></param>
    public void SetStatus(PlayerLobbyStatus status) {
        if(PlayerStatuses.TryGetValue(status, out string newStatus)) {
            _lobbyStatus.text = newStatus;
        }
    }
    
    /// <summary>
    /// Handle the ready up button click
    /// Disables the button and triggers an event to update the lobby
    /// This lets the host know that this player is ready
    /// </summary>
    private void OnReadyUpBtnClick() {

        _currentStatus = _currentStatus == PlayerLobbyStatus.ReadyUp
            ? PlayerLobbyStatus.WaitingForHost
            : PlayerLobbyStatus.ReadyUp;

        
        SetStatus(_currentStatus);
        
        ReadyUp?.Invoke();
    }
}