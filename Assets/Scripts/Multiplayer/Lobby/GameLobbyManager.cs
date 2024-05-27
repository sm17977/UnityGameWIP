using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Multiplayer;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class GameLobbyManager : MonoBehaviour {
   
    private LobbyManager lobbyManager;
    private Lobby lobby;
    private string playerId;
    private List<String> joinedLobbyList;
    private List<LobbyPlayerData> _lobbyPlayerDatas = new List<LobbyPlayerData>();
    private List<Lobby> _localLobbyList;
    private LobbyPlayerData _localLobbyPlayerData;
    private ILobbyEvents _lobbyEvents;

    public GameObject uIControllerGameObject;
    private MultiplayerUIController _uiController;
    private LobbyPlayerData _playerData;

    // Rate Limit Timers

    float getJoinedLobbiesTimer; 
    float getJoinedLobbiesLimit = 30f;
    bool canRequestGetJoinedLobbies = true;
    
    void Awake(){
#if DEDICATED_SERVER
        gameObject.SetActive(false);
        return;
#endif
        lobbyManager = LobbyManager.Instance;
        _uiController = uIControllerGameObject.GetComponent<MultiplayerUIController>();
    }

    async void Start() {
        playerId = await lobbyManager.SignInUser();
    }
    
    private void Update(){
        
        if(getJoinedLobbiesTimer > 0){
            getJoinedLobbiesTimer -= Time.deltaTime;
        }
        else{
            canRequestGetJoinedLobbies = true;
        }
    }

    public async Task CreateLobby(string lobbyName) {

        // Create a lobby
        _playerData = new LobbyPlayerData();
        _playerData.Initialize(AuthenticationService.Instance.PlayerId, "Host", "");
        int maxPlayers = 4;
        lobby = await lobbyManager.CreateLobby(lobbyName, maxPlayers, _playerData.Serialize());
        
        // Subscribe to lobby events
        var callbacks = new LobbyEventCallbacks();
        callbacks.LobbyChanged += async(change) => await _uiController.OnLobbyChanged(change);
        _lobbyEvents = await Lobbies.Instance.SubscribeToLobbyEventsAsync(lobby.Id, callbacks);
        
        // Initiate lobby heartbeat
        StartCoroutine(HeartbeatLobbyCoroutine(lobby.Id, 15));
    }

    IEnumerator HeartbeatLobbyCoroutine(string lobbyId, float waitTimeSeconds){
        var delay = new WaitForSecondsRealtime(waitTimeSeconds);

        while (true)
        {
            LobbyService.Instance.SendHeartbeatPingAsync(lobbyId.ToString());
            yield return delay;
        }
    }

    public async Task<List<Lobby>> GetLobbiesList(){
        _localLobbyList = await lobbyManager.GetLobbiesList();
        return _localLobbyList;
    }
    
    public async Task<List<Lobby>> RefreshLobbyList(){
        if (_localLobbyList == null) return await GetLobbiesList();
        return _localLobbyList;
    }
    
    public async Task<List<Player>> GetLobbyPlayers() {
        lobby = await GetLobby(lobby.Id);
        return lobby.Players;
    }

    public async Task<List<Player>> RefreshLobbyPlayers() {
        if (lobby == null) return await GetLobbyPlayers();
        return lobby.Players;
    }
    
    private async Task<Lobby> GetLobby(string lobbyId) {
        return await lobbyManager.GetLobby(lobbyId);
    }

    public async Task JoinLobby(Lobby lobbyToJoin){
        _playerData = new LobbyPlayerData();
        _playerData.Initialize(AuthenticationService.Instance.PlayerId, "Player", "");
        lobby = await lobbyManager.JoinLobby(lobbyToJoin, _playerData.Serialize());
    }
    public async Task LeaveLobby() {
        await lobbyManager.LeaveLobby(lobby.Id);
    }
    public async Task<bool> IsPlayerInLobby(Lobby lobbyToCheck){

        if(canRequestGetJoinedLobbies){
            joinedLobbyList = await lobbyManager.GetJoinedLobbies();
            canRequestGetJoinedLobbies = false;
            getJoinedLobbiesTimer = getJoinedLobbiesLimit;
        }

        foreach(string lobbyId in joinedLobbyList){
            if(lobbyToCheck.Id == lobbyId){
                return true;
            }
        }
        return false;
    }
    
    public bool IsPlayerInLobby() {

        if (lobby == null) return false;
        
        foreach (var player in lobby.Players) {
            if (player.Id == playerId) {
                return true;
            }
        }
        return false;
    }

    public async Task UpdateLobbyWithServerInfo(string machineStatus, string serverIP, int port) {
        
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
                    value: port.ToString(),
                    index: DataObject.IndexOptions.S3)
            },
        };
        
        await lobbyManager.UpdateLobbyData(lobbyData, lobby.Id);
    }

    public string GetPlayerID(){
        return playerId;
    }
    
    public bool IsPlayerHost() {
        return lobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    public bool IsPlayerHost(string id) {
        return lobby.HostId == id;
    }
    
    public void ApplyLobbyChanges(ILobbyChanges changes) {
        changes.ApplyToLobby(lobby);
    }

    public async Task UpdatePlayerDataWithClientId(ulong clientId) {
        UpdatePlayerOptions options = new UpdatePlayerOptions();
        _playerData.ClientId = clientId.ToString();
        options.Data = _playerData.SerializeUpdate();
        lobby = await lobbyManager.UpdateLobbyPlayerData(options, AuthenticationService.Instance.PlayerId, lobby.Id);
    }
    
    public async Task UpdatePlayerDataWithConnectionStatus(bool connected, string clientId) {

        LobbyPlayerData playerData = new LobbyPlayerData();
        UpdatePlayerOptions options = new UpdatePlayerOptions();
        string playerId = null;
        
        foreach(Player player in lobby.Players) {
            Debug.Log("Client ID: " + player.Data["ClientId"].Value);
            Debug.Log("Id: " + player.Data["Id"].Value);
            if (player.Data["ClientId"].Value == clientId) {
                playerId = player.Data["Id"].Value;
                var playerName = player.Data["Name"].Value;
                playerData.Initialize(playerId, playerName, clientId);
                break;
            }
        }

        playerData.IsConnected = connected;
        options.Data = playerData.SerializeUpdate();
        lobby = await lobbyManager.UpdateLobbyPlayerData(options, playerId, lobby.Id);
    }

    public string GetLobbyData(string key) {
        try {
            if (lobby.Data.TryGetValue(key, out var data)) {
                return data.Value;
            }
        }
        catch (Exception e) {
            Debug.Log(e.Message);
        }

        return null;
    }
}
