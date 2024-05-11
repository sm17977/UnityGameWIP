using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;


public class GameLobbyManager : MonoBehaviour
{
   
    public LobbyManager lobbyManager;
    private Lobby lobby;

    void Awake(){
        lobbyManager = LobbyManager.Instance;
    }

    async void Start(){
        await UnityServices.InitializeAsync();
        await SignInCachedUserAsync();

    }

    private void Update(){
        
    }

    async Task SignInCachedUserAsync(){

        try{
            if (!AuthenticationService.Instance.IsSignedIn){
                AuthenticationService.Instance.ClearSessionToken();
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("Signed in user: " + AuthenticationService.Instance.PlayerId);
            }
        }
        catch(AuthenticationException e){
            Debug.LogException(e);
        }
    }

    public async Task CreateLobby(){
        string lobbyName = "MyLobby";
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

    public async Task JoinLobby(Lobby lobby){
        await lobbyManager.JoinLobby(lobby);
    }

    public async Task<bool> IsPlayerInLobby(Lobby lobby){
        var joinedLobbies = await lobbyManager.GetJoinedLobbies();

        foreach(string lobbyId in joinedLobbies){
            if(lobby.Id == lobbyId){
                return true;
            }
        }
        return false;
    }
}
