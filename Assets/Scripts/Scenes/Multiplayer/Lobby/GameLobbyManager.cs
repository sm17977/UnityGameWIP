using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Multiplayer;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public delegate void ShowMessage();
public class GameLobbyManager : MonoBehaviour {

    public event ShowMessage OnShowMessage;

    public static GameLobbyManager Instance;
    private LobbyManager _lobbyManager;
    
    private Lobby _lobby;
    private string _playerId;
    private LobbyPlayerData _playerData;
    
    private List<String> _joinedLobbyList;
    private List<Lobby> _localLobbyList;
    
    private List<LobbyPlayerData> _lobbyPlayerDatas = new List<LobbyPlayerData>();
    private LobbyPlayerData _localLobbyPlayerData;
    private ILobbyEvents _lobbyEvents;

    public GameObject uIControllerGameObject;
    private MultiplayerUIController _uiController;

    public bool gameStarted;
    
    public ServerProvisionState currentServerProvisionState = ServerProvisionState.Idle;
    
    // Rate Limit Timers
    float getJoinedLobbiesTimer; 
    float getJoinedLobbiesLimit = 30f;
    bool canRequestGetJoinedLobbies = true;
    
    void Awake(){
        #if DEDICATED_SERVER
            gameObject.SetActive(false);
            return;
        #endif
        
        if(Instance == null){
            Instance = this;
        }
        else if(Instance != this){
            Destroy(this);
        }
        
        _lobbyManager = LobbyManager.Instance;
        _uiController = uIControllerGameObject.GetComponent<MultiplayerUIController>();
    }
    
    private void Update(){
        
        if(getJoinedLobbiesTimer > 0){
            getJoinedLobbiesTimer -= Time.deltaTime;
        }
        else{
            canRequestGetJoinedLobbies = true;
        }
    }
    
    /// <summary>
    /// Sign in the user to Unity Authentication Services
    /// </summary>
    /// <returns>Unity Authenticated player ID</returns>
    public async Task<string> SignIn() {
        _playerId = await _lobbyManager.SignInUser();
        return _playerId;
    }

    /// <summary>
    /// Is the player signed in to Unity Authentication Services
    /// <returns>boolean</returns>
    /// </summary>
    public bool IsPlayerSignedIn() {
        try {
            return AuthenticationService.Instance.IsSignedIn;
        }
        catch (Exception) {
            return false;
        }
    }
    
    /// <summary>
    /// Send a regular heartbeat ping to the lobby to keep it alive 
    /// </summary>
    IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds){
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);

        while (true) {
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId.ToString());
            yield return delay;
        }
    }

    /// <summary>
    /// Create and store reference to a lobby
    /// <param name="lobbyName">Lobby name</param>
    /// <param name="maxPlayers">Max player count</param>
    /// </summary>
    public async Task<bool> CreateLobby(string lobbyName, int maxPlayers, string gameMode) {
        
        _playerData = new LobbyPlayerData();
        _playerData.Initialize(AuthenticationService.Instance.PlayerId, "Host", "");
        
        // Create a lobby
        _lobby = await _lobbyManager.CreateLobby(lobbyName, maxPlayers, gameMode, _playerData.Serialize());

        if (_lobby == null) {
            OnShowMessage?.Invoke();
            return false;
        }
        
        // Subscribe to lobby events
        var callbacks = new LobbyEventCallbacks();
        callbacks.LobbyChanged += async(change) => _uiController.OnLobbyChanged(change);
        _lobbyEvents = await _lobbyManager.SubscribeToLobbyEvents(_lobby.Id, callbacks);
        
        // Initiate lobby heartbeat
        StartCoroutine(HeartbeatLobbyCoroutine(_lobby.Id, 15));
        return true;
    }
    
    /// <summary>
    /// Join and store a reference to a lobby
    /// </summary>
    /// TODO: Check if we need the clientId in playerData
    public async Task<bool> JoinLobby(Lobby lobbyToJoin){
        
        _playerData = new LobbyPlayerData();
        _playerData.Initialize(AuthenticationService.Instance.PlayerId, "Player", "");
        
        _lobby = await _lobbyManager.JoinLobby(lobbyToJoin, _playerData.Serialize());

        if (_lobby == null) {
            OnShowMessage?.Invoke();
            return false;
        }
        
        // Subscribe to lobby events
        var callbacks = new LobbyEventCallbacks();
        callbacks.LobbyChanged += async(change) => _uiController.OnLobbyChanged(change);
        _lobbyEvents = await _lobbyManager.SubscribeToLobbyEvents(_lobby.Id, callbacks);
        return true;
    }
    
    /// <summary>
    /// Leave a lobby
    /// </summary>
    public async Task<bool> LeaveLobby() {
        await _lobbyManager.UnsubscribeToLobbyEvents(_lobbyEvents);
        var success = await _lobbyManager.LeaveLobby(_lobby.Id);
        if (!success) {
            OnShowMessage?.Invoke();
            return false;
        }
        _lobby = null;
        return true;
    }

    /// <summary>
    /// Delete the current lobby
    /// </summary>
    public async Task<bool> DeleteLobby() {
        await _lobbyManager.UnsubscribeToLobbyEvents(_lobbyEvents);
        var success = await _lobbyManager.DeleteLobby(_lobby.Id);
        if (!success) {
            OnShowMessage?.Invoke();
            return false;
        }
        _lobby = null;
        return true;
    }
    
    /// <summary>
    /// Invalidate the current lobby reference
    /// </summary>
    public void InvalidateLobby() {
        _lobby = null;
    }
    
    /// <summary>
    /// Get a lobby via the Lobby Service
    /// </summary>
    /// <returns>A lobby</returns>
    private async Task<Lobby> GetLobby(string lobbyId) {
        var lobby = await _lobbyManager.GetLobby(lobbyId);
        if(lobby == null) OnShowMessage?.Invoke();
        return lobby;
    }
    
    /// <summary>
    /// Get a field from the current lobby data
    /// </summary>
    /// <param name="key">The field key of the lobby data to retrieve</param>
    /// <returns>The requested lobby data</returns>
    public string GetLobbyData(string key) {
        try {
            return _lobby.Data[key].Value;
        }
        catch (Exception e) {
            Debug.Log(e.Message);
        }
        return null;
    }
    
    /// <summary>
    /// Get and store the list of lobbies via the Lobby Service
    /// </summary>
    /// <returns>List of lobbies</returns>
    public async Task<List<Lobby>> GetLobbiesList(){
       var lobbies = await _lobbyManager.GetLobbiesList();
       if(lobbies == null) OnShowMessage?.Invoke();
       return lobbies;
    }
    
    /// <summary>
    /// Returns the local lobbies list 
    /// </summary>
    /// <returns>Local lobby list</returns>
    public async Task<List<Lobby>> GetLocalLobbiesList(){
        return _localLobbyList;
    }
    
    /// <summary>
    /// Update the lobby data with the multiplay server's network info
    /// </summary>
    /// <param name="machineStatus">Machine status</param>
    /// <param name="serverIP">Server IPv4 address</param>
    /// <param name="port">Server Port</param>
    public async Task UpdateLobbyWithServerInfo(string machineStatus, string serverIP, string port) {
        
        var lobbyData = new Dictionary<string, DataObject>() {
            {
                "MachineStatus", new DataObject(
                    visibility: DataObject.VisibilityOptions.Public,
                    value: machineStatus,
                    index: DataObject.IndexOptions.S1)
            }, 
            {
                "ServerIP", new DataObject(
                    visibility: DataObject.VisibilityOptions.Public,
                    value: serverIP,
                    index: DataObject.IndexOptions.S2)
            },
            {
                "Port", new DataObject(
                    visibility: DataObject.VisibilityOptions.Public,
                    value: port,
                    index: DataObject.IndexOptions.S3)
            },
        };
        
        var lobby = await _lobbyManager.UpdateLobbyData(lobbyData, _lobby.Id);
        if (lobby != null){
            _lobby = lobby;
        }
        else OnShowMessage?.Invoke();
    }

    public async Task UpdateLobbyWithGameMode(string gamemode) {

        var lobbyData = new Dictionary<string, DataObject>() {
            {
                "GameMode", new DataObject(
                    visibility: DataObject.VisibilityOptions.Public,
                    value: gamemode,
                    index: DataObject.IndexOptions.S4)
            }
        };

        var lobby = await _lobbyManager.UpdateLobbyData(lobbyData, _lobby.Id);
        if (lobby != null) {
            _lobby = lobby;
        }
        else OnShowMessage?.Invoke();
    }
    
    public async Task UpdateLobbyWithGameStart(bool startGame) {

        var lobbyData = new Dictionary<string, DataObject>() {
            {
                "StartGame", new DataObject(
                    visibility: DataObject.VisibilityOptions.Public,
                    value: startGame.ToString(),
                    index: DataObject.IndexOptions.S4)
            }
        };

        var lobby = await _lobbyManager.UpdateLobbyData(lobbyData, _lobby.Id);
        if (lobby != null) {
            _lobby = lobby;
        }
        else OnShowMessage?.Invoke();
    }
    
    /// <summary>
    /// Get the lobby via the Lobby Service and return the player list
    /// </summary>
    /// <returns>Lobby player list</returns>
    public async Task<List<Player>> GetLobbyPlayers() {
        _lobby = await GetLobby(_lobby.Id);
        if(_lobby != null) return _lobby.Players;
        OnShowMessage?.Invoke();
        return null;
    }

    /// <summary>
    /// Get local lobby player list
    /// </summary>
    /// <returns>Local lobby player list</returns>
    public async Task<List<Player>> GetLocalLobbyPlayers() {
        return _lobby.Players;
    }
    
    /// <summary>
    /// Update each lobby player's data with their multiplay server connection status
    /// </summary>
    /// <param name="connected">If the player connected to the multiplay server</param>
    /// TODO: Move this into to the ClientManager?
    public async Task UpdatePlayerDataWithConnectionStatus(bool connected) {

        LobbyPlayerData playerData = new LobbyPlayerData();
        UpdatePlayerOptions options = new UpdatePlayerOptions();
        
        foreach(Player player in _lobby.Players) {
            if (player.Id == _playerId) {
                var clientId = player.Data["ClientId"].Value;
                var playerName = player.Data["Name"].Value;
                playerData.Initialize(_playerId, playerName, clientId, connected);
                break;
            }
        }
        options.Data = playerData.SerializeUpdate();
        var lobby = await _lobbyManager.UpdateLobbyPlayerData(options, _playerId, _lobby.Id);
        if (lobby != null) {
            _lobby = lobby;
        }
        else OnShowMessage?.Invoke();
    }
    
    /// <summary>
    /// Get the player's ID
    /// </summary>
    /// <returns>The player's ID</returns>
    public string GetPlayerID(){
        return _playerId;
    }
    
    /// <summary>
    /// Check if the player is in a certain lobby
    /// </summary>
    /// <param name="lobbyToCheck">The lobby to check</param>
    /// <returns>boolean</returns>
    public async Task<bool> IsPlayerInLobby(Lobby lobbyToCheck){

        if(canRequestGetJoinedLobbies){
            _joinedLobbyList = await _lobbyManager.GetJoinedLobbies();
            if (_joinedLobbyList == null) return false;
            canRequestGetJoinedLobbies = false;
            getJoinedLobbiesTimer = getJoinedLobbiesLimit;
        }

        foreach(string lobbyId in _joinedLobbyList){
            if(lobbyToCheck.Id == lobbyId){
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Check if the player is in the current lobby
    /// </summary>
    /// <returns>boolean</returns>
    public bool IsPlayerInLobby() {

        if (_lobby == null) return false;
        
        foreach (var player in _lobby.Players) {
            if (player.Id == _playerId) {
                return true;
            }
        }
        return false;
    }
    
    /// <summary>
    /// Check if the player is the host of the current lobby
    /// </summary>
    /// <returns>boolean</returns>
    public bool IsPlayerLobbyHost() {
        return _lobby.HostId == _playerId;
    }

    /// <summary>
    /// Check if a player is the host of the current lobby
    /// </summary>
    /// <param name="playerId">Lobby ID</param>
    /// <returns>boolean</returns>
    public bool IsPlayerLobbyHost(string playerId) {
        return _lobby.HostId == playerId;
    }
    
    /// <summary>
    /// Check if lobby host is connected to the multiplay server
    /// </summary>
    /// <returns>boolean</returns>
    public bool HostIsConnected() {
        foreach(var player in _lobby.Players) {
            if (player.Id == _lobby.HostId) {
                var connected = bool.Parse(player.Data["IsConnected"].Value);
                if (connected) {
                    return true;
                }
                break;
            }
        }
        return false;
    }

    /// <summary>
    /// Check if a specific player is connected to the multiplay server
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns>boolean</returns>
    private bool IsConnected(string playerId) {
        foreach(var player in _lobby.Players) {
            if (player.Id == playerId) {
                var connected = bool.Parse(player.Data["IsConnected"].Value);
                if (connected) {
                    return true;
                }
                break;
            }
        }
        return false;
    }

    /// <summary>
    /// Get a list of lobby player IDs of all players connected to the multiplay server
    /// </summary>
    /// <returns></returns>
    public List<string> GetConnectedPlayers() {
        List<string> connectedPlayers = new List<string>();
        foreach (var player in _lobby.Players) {
            if (IsConnected(player.Id)) {
                connectedPlayers.Add(player.Id);
            }
        }
        return connectedPlayers;
    }
    

    /// <summary>
    /// Check if all lobby players have disconnected from the multiplay server
    /// </summary>
    /// <returns>boolean</returns>
    public bool LobbyPlayersDisconnected() {
        foreach(var player in _lobby.Players) {
            if (player.Id != _lobby.HostId) {
                var connected = bool.Parse(player.Data["IsConnected"].Value);
                if (connected) return false;
            }
        }
        return true;
    }
    
    /// <summary>
    /// Apply changes to a lobby
    /// </summary>
    /// <param name="changes">Lobby changes</param>
    public void ApplyLobbyChanges(ILobbyChanges changes) {
        changes.ApplyToLobby(_lobby);
    }

    /// <summary>
    /// Get number of players in the current lobby
    /// </summary>
    /// <returns>The number of players currently in the lobby</returns>
    public int GetLobbyPlayerCount() {
        return _lobby.Players.Count;
    }
    
    /// <summary>
    /// Get current game mode 
    /// </summary>
    /// <returns>The name of the current lobby's game mode</returns>
    public string GetLobbyGameMode() {
        return GetLobbyData("GameMode");
    }

    public bool PlayersReady() {
        foreach (var player in _lobby.Players) {
            if (player.Id != _lobby.HostId && !bool.Parse(player.Data["IsReady"].Value)) return false;
        }
        return true;
    }

    public async Task UpdatePlayerIsReadyData() {
        LobbyPlayerData playerData = new LobbyPlayerData();
        UpdatePlayerOptions options = new UpdatePlayerOptions();
        
        foreach(Player player in _lobby.Players) {
            if (player.Id == _playerId) {
                var clientId = player.Data["ClientId"].Value;
                var playerName = player.Data["Name"].Value;
                var connected = bool.Parse(player.Data["IsConnected"].Value);
                var ready = !bool.Parse(player.Data["IsReady"].Value);
                playerData.Initialize(_playerId, playerName, clientId, connected, ready);
                break;
            }
        }
        options.Data = playerData.SerializeUpdate();
        var lobby = await _lobbyManager.UpdateLobbyPlayerData(options, _playerId, _lobby.Id);
        if (lobby != null) {
            _lobby = lobby;
        }
        else OnShowMessage?.Invoke();
    }
    
    public async Task UpdatePlayerIsAliveData() {
        LobbyPlayerData playerData = new LobbyPlayerData();
        UpdatePlayerOptions options = new UpdatePlayerOptions();
        
        foreach(Player player in _lobby.Players) {
            if (player.Id == _playerId) {
                var clientId = player.Data["ClientId"].Value;
                var playerName = player.Data["Name"].Value;
                var connected = bool.Parse(player.Data["IsConnected"].Value);
                var ready = !bool.Parse(player.Data["IsReady"].Value);
                playerData.Initialize(_playerId, playerName, clientId, connected, ready, false);
                break;
            }
        }
        options.Data = playerData.SerializeUpdate();
        var lobby = await _lobbyManager.UpdateLobbyPlayerData(options, _playerId, _lobby.Id);
        if (lobby != null) {
            _lobby = lobby;
        }
        else OnShowMessage?.Invoke();
    }
    
    
}
