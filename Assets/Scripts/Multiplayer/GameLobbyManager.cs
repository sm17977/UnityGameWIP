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
    private LobbyPlayerData _localLobbyPlayerData;

    // Rate Limit Timers

    float getJoinedLobbiesTimer; 
    float getJoinedLobbiesLimit = 30f;
    bool canRequestGetJoinedLobbies = true;
    
    void Awake(){
        lobbyManager = LobbyManager.Instance;
    }

    async void Start(){
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

        LobbyPlayerData playerData = new LobbyPlayerData();
        playerData.Initialize(AuthenticationService.Instance.PlayerId, "Host");
        int maxPlayers = 4;
        lobby = await lobbyManager.CreateLobby(lobbyName, maxPlayers, playerData.Serialize());
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
        return await lobbyManager.GetLobbiesList();
    }

    private async Task<Lobby> GetLobby(string lobbyId) {
        return await lobbyManager.GetLobby(lobbyId);
    }

    public async Task JoinLobby(Lobby lobbyToJoin){
        LobbyPlayerData playerData = new LobbyPlayerData();
        playerData.Initialize(AuthenticationService.Instance.PlayerId, "Player");
        lobby = await lobbyManager.JoinLobby(lobbyToJoin, playerData.Serialize());
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

    public async Task UpdateLobbyWithServerInfo(string machineStatus, string serverIP) {

        if (!IsLobbyHost()) return;
        
        var lobbyData = new Dictionary<string, DataObject>() {
            {
                "MachineStatus", new DataObject(
                    visibility: DataObject.VisibilityOptions.Public,
                    value: machineStatus,
                    index: DataObject.IndexOptions.S1)
            }, {
                "ServerIP", new DataObject(
                    visibility: DataObject.VisibilityOptions.Public,
                    value: serverIP,
                    index: DataObject.IndexOptions.S2)
            }
        };
        
        await lobbyManager.UpdateLobbyData(lobbyData, lobby.Id);
    }

    public string GetPlayerID(){
        return playerId;
    }
    public string GetLobbyId() {
        return lobby.Id;
    }

    private bool IsLobbyHost() {
        return lobby.HostId == playerId;
    }

    public bool IsPlayerHost(string id) {
        return lobby.HostId == id;
    }

    public async Task<List<Player>> GetLobbyPlayers() {
        lobby = await GetLobby(lobby.Id);
        return lobby.Players;
    }
}
