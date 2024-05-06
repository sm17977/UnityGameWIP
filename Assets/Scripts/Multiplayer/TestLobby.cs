using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using QFSW.QC;
using UnityEditor;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

public class TestLobby : MonoBehaviour
{
   
    private Lobby hostLobby;
    private float heartbeatTimer;
    private bool isJoining = false;

    async void Start(){
        heartbeatTimer = 15f;
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () => {
            Debug.Log("Signed in user: " + AuthenticationService.Instance.PlayerId);
        };


        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private void Update(){
        HandleLobbyHeartbeat();
    }

    private async void HandleLobbyHeartbeat(){
        if(hostLobby != null){
            heartbeatTimer -= Time.deltaTime;
            if(heartbeatTimer < 0f){
                float heartbeatTimerMax = 15f;
                heartbeatTimer = heartbeatTimerMax;
                Debug.Log("Beat");
                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }

    [Command]
    private async void CreateLobby(){
        try{
            string lobbyName = "MyLobby";
            int maxPlayers = 4;
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers);

            hostLobby = lobby;
            
            Debug.Log("Created new lobby: " + lobbyName);
        }
        catch(LobbyServiceException e){
            Debug.Log(e);
        }
    }

    [Command]
    private async void ListLobbies(){
        try{
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();
            Debug.Log("Lobbies found: " + queryResponse.Results.Count);

            foreach(Lobby lobby in queryResponse.Results){
                Debug.Log("Lobby Name: " + lobby.Name);
            }
        }
        catch(LobbyServiceException e){
            Debug.Log(e);
        }
    }

    public async Task<List<Lobby>> GetLobbiesList(){
        try{
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();
            Debug.Log("Lobbies found: " + queryResponse.Results.Count);

            foreach(Lobby lobby in queryResponse.Results){
                Debug.Log("Lobby Name: " + lobby.Name);
            }

            return queryResponse.Results.ToList<Lobby>();
        }
        catch(LobbyServiceException e){
            Debug.Log(e);
        }
        return new List<Lobby>();
    }



    [Command]
    private async void JoinLobby(){

        if(isJoining) return;

        isJoining = true;

        try{
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();
            await Lobbies.Instance.JoinLobbyByIdAsync(queryResponse.Results[0].Id);
        }
        catch (LobbyServiceException e){
            Debug.Log(e);
        }

        isJoining = false;
    }


    public async void Join(Lobby lobby){

        if(isJoining) return;

        isJoining = true;

        try{
            await Lobbies.Instance.JoinLobbyByIdAsync(lobby.Id);
        }
        catch (LobbyServiceException e){
            Debug.Log(e);
        }

        isJoining = false;
    }








}
