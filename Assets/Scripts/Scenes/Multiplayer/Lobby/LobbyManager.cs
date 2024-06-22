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
        
        System.Random random = new System.Random();
        int randomNumber = random.Next();
        
        var options = new InitializationOptions();
        options.SetProfile("Player" +  randomNumber.ToString());
        await UnityServices.InitializeAsync(options);
        
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

    // 2 requests per 6 seconds
    public async Task<Lobby> CreateLobby(string lobbyName, int maxPlayers, Dictionary<string, string> data) {

        Dictionary<string, PlayerDataObject> playerData = SerializePlayerData(data);
        Player player = new Player(AuthenticationService.Instance.PlayerId, null, playerData);

        CreateLobbyOptions options = new CreateLobbyOptions() {
            IsPrivate = false,
            Player = player
        };
        
        lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
        Debug.Log("Created new lobby: " + lobbyName);
        return lobby;
    }
    
    // 1 request per second
    public async Task<List<Lobby>> GetLobbiesList(){
        try{
            QueryResponse queryResponse = await Lobbies.Instance.QueryLobbiesAsync();
            //Debug.Log("Lobbies found: " + queryResponse.Results.Count);

            foreach(Lobby lobby in queryResponse.Results){
               // Debug.Log("Lobby Name: " + lobby.Name);
            }

            return queryResponse.Results.ToList<Lobby>();
        }
        catch(LobbyServiceException e){
            Debug.Log(e);
        }
        return new List<Lobby>();
    }
    
    // 1 request per second
    public async Task<Lobby> GetLobby(string lobbyId) {
        try {
            return await LobbyService.Instance.GetLobbyAsync(lobbyId);
        }
        catch (LobbyServiceException e) {
            Debug.Log(e);
        }

        return null;
    }

    // 2 requests per 6 seconds
    public async Task<Lobby> JoinLobby(Lobby lobby, Dictionary<string, string> data){
        
        Dictionary<string, PlayerDataObject> playerData = SerializePlayerData(data);
        Player player = new Player(AuthenticationService.Instance.PlayerId, null, playerData);
        
        try {
            JoinLobbyByIdOptions joinLobbyByIdOptions = new JoinLobbyByIdOptions() {
                Player = player
            };
            lobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobby.Id, joinLobbyByIdOptions);
            Debug.Log("Joined lobby");
            return lobby;
        }
        catch (LobbyServiceException e){
            Debug.Log(e);
        }

        return null;
    }

    // 5 requests per second
    public async Task LeaveLobby(string lobbyId) {
        try {
            string playerId = AuthenticationService.Instance.PlayerId;
            await LobbyService.Instance.RemovePlayerAsync(lobbyId, playerId);
        }
        catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    // 1 request per 30 seconds
    public async Task<List<String>> GetJoinedLobbies(){

        try{
            return await LobbyService.Instance.GetJoinedLobbiesAsync();
        }
        catch (LobbyServiceException e){
            Debug.Log(e);
            return new List<string>();
        }
    }
    
    // 5 requests per 5 seconds
    public async Task UpdateLobbyData(Dictionary<string, DataObject> lobbyData, string lobbyId) {
        
        try {
            var options = new UpdateLobbyOptions() {
                HostId = AuthenticationService.Instance.PlayerId,
                Data = lobbyData
            };
            
            await LobbyService.Instance.UpdateLobbyAsync(lobbyId, options);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private Dictionary<string, PlayerDataObject> SerializePlayerData(Dictionary<string, string> data) {
        Dictionary<string, PlayerDataObject> playerData = new Dictionary<string, PlayerDataObject>();
        foreach (var (key, value) in data) {
            playerData.Add(key, new PlayerDataObject(
                visibility: PlayerDataObject.VisibilityOptions.Member,
                value: value));
        }

        return playerData;
    }

    public  List<Dictionary<string, PlayerDataObject>> GetPlayerData() {
        List<Dictionary<string, PlayerDataObject>> data = new List<Dictionary<string, PlayerDataObject>>();
        foreach (Player player in lobby.Players) {
            data.Add(player.Data);
        }

        return data;
    }

    // 5 requests per 5 seconds
    public async Task<Lobby> UpdateLobbyPlayerData(UpdatePlayerOptions options, string playerId, string lobbyId) {
        try {
            var lobby = await LobbyService.Instance.UpdatePlayerAsync(lobbyId, playerId, options);
            return lobby;
        }
        catch (LobbyServiceException e) {
            Debug.Log(e);
        }

        return null;
    }

    public async Task DeleteLobby(string lobbyId) {
        try {
            await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
        }
        catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }
    
    public void OnApplicationQuit(){
        if(lobby != null && lobby.HostId == AuthenticationService.Instance.PlayerId){
            LobbyService.Instance.DeleteLobbyAsync(lobby.Id);
        }
    }















}