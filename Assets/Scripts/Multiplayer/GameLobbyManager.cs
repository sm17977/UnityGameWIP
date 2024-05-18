using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;


public class GameLobbyManager : MonoBehaviour {
   
    private LobbyManager lobbyManager;
    private Lobby lobby;
    private string playerId;
    private List<String> joinedLobbyList;

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

    public async Task CreateLobby(string lobbyName){
        int maxPlayers = 4;
        lobby = await lobbyManager.CreateLobby(lobbyName, maxPlayers);
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

    public async Task JoinLobby(Lobby lobbyToJoin){
        await lobbyManager.JoinLobby(lobbyToJoin);
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

    public string GetPlayerID(){
        return playerId;
    }

}
