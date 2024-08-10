using Unity.Services.Lobbies;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;

public sealed class LobbyManager {
    private static LobbyManager _instance = null;
    private static readonly object Padlock = new object();
    
    public static LobbyManager Instance {
        get {
            lock (Padlock) {
                _instance ??= new LobbyManager();
                return _instance;
            }
        }
    }
    
    /// <summary>
    /// Sign in the user to Unity Authentication Services
    /// </summary>
    /// <returns></returns>
    public async Task<string> SignInUser() {
        var random = new System.Random();
        var randomNumber = random.Next();

        var options = new InitializationOptions();
        options.SetProfile("Player" + randomNumber.ToString());
        await UnityServices.InitializeAsync(options);

        try {
            if (!AuthenticationService.Instance.IsSignedIn) {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("Signed in user: " + AuthenticationService.Instance.PlayerId);
            }

            return AuthenticationService.Instance.PlayerId;
        }
        catch (AuthenticationException e) {
            Debug.LogException(e);
            return "";
        }
    }
    
    /// <summary>
    /// Create a lobby via the Lobby Service
    /// 2 requests per 6 seconds
    /// </summary>
    /// <returns>The created lobby</returns>
    public async Task<Lobby> CreateLobby(string lobbyName, int maxPlayers, string gameMode, Dictionary<string, string> data) {
        var playerData = SerializePlayerData(data);
        var player = new Player(AuthenticationService.Instance.PlayerId, null, playerData);
       
        var options = new CreateLobbyOptions() {
            IsPrivate = false,
            Player = player,
            Data = new Dictionary<string, DataObject>() {
                {
                    "GameMode", new DataObject(
                        DataObject.VisibilityOptions.Public,
                         value: gameMode)
                }
            }
        };
        
        var lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
        Debug.Log("Created new lobby: " + lobbyName);
        return lobby;
    }
    
    /// <summary>
    /// Join a lobby via the Lobby Service
    /// 2 requests per 6 seconds
    /// </summary>
    /// <returns>The joined lobby</returns>
    public async Task<Lobby> JoinLobby(Lobby lobby, Dictionary<string, string> data) {
        var playerData = SerializePlayerData(data);
        var player = new Player(AuthenticationService.Instance.PlayerId, null, playerData);

        try {
            var joinLobbyByIdOptions = new JoinLobbyByIdOptions() {
                Player = player
            };
            lobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobby.Id, joinLobbyByIdOptions);
            return lobby;
        }
        catch (LobbyServiceException e) {
            Debug.Log(e);
        }

        return null;
    }
    
    /// <summary>
    /// Leave a lobby via the Lobby Service
    /// 5 requests per second
    /// </summary>
    public async Task LeaveLobby(string lobbyId) {
        try {
            var playerId = AuthenticationService.Instance.PlayerId;
            await LobbyService.Instance.RemovePlayerAsync(lobbyId, playerId);
        }
        catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }
    
    /// <summary>
    /// Delete a lobby via the Lobby Service
    /// </summary>
    public async Task DeleteLobby(string lobbyId) {
        try {
            await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
        }
        catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }
    
    /// <summary>
    /// Get a lobby by its ID using the Lobby Service
    /// 1 request per second
    /// </summary>
    /// <returns>The requested lobby</returns>
    public async Task<Lobby> GetLobby(string lobbyId) {
        try {
            return await LobbyService.Instance.GetLobbyAsync(lobbyId);
        }
        catch (LobbyServiceException e) {
            Debug.Log(e);
        }
        return null;
    }

    /// <summary>
    /// Get a list of all lobbies via the Lobby Service
    /// 1 request per second
    /// </summary>
    /// <returns>List of lobbies</returns>
    public async Task<List<Lobby>> GetLobbiesList() {
        try {
            var queryResponse = await Lobbies.Instance.QueryLobbiesAsync();
            return queryResponse.Results.ToList<Lobby>();
        }
        catch (LobbyServiceException e) {
            Debug.Log(e);
        }
        return new List<Lobby>();
    }
    
    /// <summary>
    /// Get a list of currently joined lobbies
    /// 1 request per 30 seconds
    /// </summary>
    /// <returns>List of joined lobbies</returns>
    public async Task<List<string>> GetJoinedLobbies() {
        try {
            return await LobbyService.Instance.GetJoinedLobbiesAsync();
        }
        catch (LobbyServiceException e) {
            Debug.Log(e);
            return new List<string>();
        }
    }
    
    /// <summary>
    /// Update a looby's data via the Lobby Service
    /// 5 requests per 5 seconds
    /// </summary>
    /// <returns>The updated lobby</returns>
    public async Task<Lobby> UpdateLobbyData(Dictionary<string, DataObject> lobbyData, string lobbyId) {
        try {
            var options = new UpdateLobbyOptions() {
                HostId = AuthenticationService.Instance.PlayerId,
                Data = lobbyData
            };
            return await LobbyService.Instance.UpdateLobbyAsync(lobbyId, options);
        }
        catch (LobbyServiceException e) {
            Debug.Log(e);
        }

        return null;
    }
    
    /// <summary>
    /// Update a lobby player's data via the Lobby Service
    /// 5 requests per 5 seconds
    /// </summary>
    /// <returns>The updated lobby</returns>
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
    
    /// <summary>
    /// Serialize a lobby player's data
    /// </summary>
    /// <returns>The serialized lobby player's data</returns>
    private Dictionary<string, PlayerDataObject> SerializePlayerData(Dictionary<string, string> data) {
        var playerData = new Dictionary<string, PlayerDataObject>();
        foreach (var (key, value) in data) {
            playerData.Add(key, new PlayerDataObject(
                PlayerDataObject.VisibilityOptions.Member,
                value));
        }
        return playerData;
    }
}