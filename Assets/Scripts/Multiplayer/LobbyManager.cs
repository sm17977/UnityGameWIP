

using Unity.Services.Lobbies;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using System;
using Unity.Services.Authentication;
using Unity.Services.Core;
public sealed class LobbyManager{

    private static LobbyManager instance = null;
    private static readonly object padlock = new object();
    private Lobby lobby;

    private LobbyManager(){
    }

    public static LobbyManager Instance{
        get{
            lock(padlock){
                if(instance == null){
                    instance = new LobbyManager();
                }
                return instance;
            }
        }
    }

    public async Task<string> SignInUser(){
        await UnityServices.InitializeAsync();
        
        try{
            if (!AuthenticationService.Instance.IsSignedIn){
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("Signed in user: " + AuthenticationService.Instance.PlayerId);
            }
            return AuthenticationService.Instance.PlayerId;
        }
        catch(AuthenticationException e){
            Debug.LogException(e);
            return "null";
        }
    }

    public async Task<Lobby> CreateLobby(string lobbyName, int maxPlayers){
        lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers);
        Debug.Log("Created new lobby: " + lobbyName);
        return lobby;
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

    public async Task JoinLobby(Lobby lobby){
        try{
            await Lobbies.Instance.JoinLobbyByIdAsync(lobby.Id);
        }
        catch (LobbyServiceException e){
            Debug.Log(e);
        }
    }

    public async Task<List<String>> GetJoinedLobbies(){

        try{
            return await LobbyService.Instance.GetJoinedLobbiesAsync();
        }
        catch (LobbyServiceException e){
            Debug.Log(e);
            return new List<string>();
        }
    }

    
    public void OnApplicationQuit(){
        if(lobby != null && lobby.HostId == AuthenticationService.Instance.PlayerId){
            LobbyService.Instance.DeleteLobbyAsync(lobby.Id);
        }
    }















}