using System;
using Unity.Services.Lobbies;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using JetBrains.Annotations;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Exception = System.Exception;

public sealed class LobbyManager {
    private static LobbyManager _instance = null;
    private static readonly object Padlock = new object();
    private Queue<Func<Task>> _requestQueue = new Queue<Func<Task>>();
    
    public static LobbyManager Instance {
        get {
            lock (Padlock) {
                _instance ??= new LobbyManager();
                return _instance;
            }
        }
    }

    private enum LobbyRequestType {
        CreateLobby,
        JoinLobby,
        LeaveLobby,
        GetLobby,
        GetLobbiesList,
        GetJoinedLobbies,
        UpdateLobbyData,
        UpdateLobbyPlayerData,
        RemovePlayer
    }

    private Dictionary<LobbyRequestType, DateTime> _lastRequestTime = new Dictionary<LobbyRequestType, DateTime>();
    private readonly Dictionary<LobbyRequestType, TimeSpan> _requestLimits = new Dictionary<LobbyRequestType, TimeSpan> {
        { LobbyRequestType.CreateLobby, TimeSpan.FromSeconds(3) },    // 2 requests per 6 seconds
        { LobbyRequestType.JoinLobby, TimeSpan.FromSeconds(3) },      // 2 requests per 6 seconds
        { LobbyRequestType.LeaveLobby, TimeSpan.FromMilliseconds(200) }, // 5 requests per second
        { LobbyRequestType.GetLobby, TimeSpan.FromSeconds(1) },       // 1 request per second
        { LobbyRequestType.GetLobbiesList, TimeSpan.FromSeconds(1) }, // 1 request per second
        { LobbyRequestType.GetJoinedLobbies, TimeSpan.FromSeconds(30) }, // 1 request per 30 seconds
        { LobbyRequestType.UpdateLobbyData, TimeSpan.FromSeconds(1) }, // 5 requests per 5 seconds
        { LobbyRequestType.UpdateLobbyPlayerData, TimeSpan.FromSeconds(1) }, // 5 requests per 5 seconds
    };
    
    private Dictionary<LobbyRequestType, bool> _isRequestInProgress = new Dictionary<LobbyRequestType, bool>
    {
        { LobbyRequestType.CreateLobby, false },
        { LobbyRequestType.JoinLobby, false },
        { LobbyRequestType.LeaveLobby, false },
        { LobbyRequestType.GetLobby, false },
        { LobbyRequestType.GetLobbiesList, false },
        { LobbyRequestType.GetJoinedLobbies, false },
        { LobbyRequestType.UpdateLobbyData, false },
        { LobbyRequestType.UpdateLobbyPlayerData, false },
        { LobbyRequestType.RemovePlayer, false }
    };
    
    /// <summary>
    /// Handle each lobby request
    /// First checks to see if an existing lobby request type is in progress
    /// If it is, the pending request is queued up to run when the current one is finished
    /// If there is no existing lobby request type in progress then it's validated to check
    /// the time of the previous request of the same type. If validation fails then it's delayed
    /// until enough time has passed to avoid a rate limit error.
    /// If validation passes the lobby request is executed
    /// </summary>
    /// <returns>boolean</returns>
    private async Task<T> HandleRequest<T>(LobbyRequestType requestType, Func<Task<T>> requestFunc) {

        if (_isRequestInProgress[requestType]) {
            var tcs = new TaskCompletionSource<T>();
            _requestQueue.Enqueue(async () => tcs.SetResult(await HandleRequest(requestType, requestFunc)));
            return await tcs.Task;
        }

        _isRequestInProgress[requestType] = true;

        try {
            if (!ValidateRequest(requestType)) {
                var delayTime = _requestLimits[requestType] - (DateTime.UtcNow - _lastRequestTime[requestType]);
                if (delayTime > TimeSpan.Zero) {
                    Debug.Log($"Throttling request. Waiting {delayTime.TotalSeconds} seconds before retrying.");
                    await Task.Delay(delayTime);
                }
            }
            
            _lastRequestTime[requestType] = DateTime.UtcNow;
            return await requestFunc();
        }
        finally {
            _isRequestInProgress[requestType] = false;
            
            if (_requestQueue.Count > 0) {
                var nextRequest = _requestQueue.Dequeue();
                await nextRequest();
            }
        }
    }

    
    /// <summary>
    /// Check if this request can be sent according to Unity's usage limits for Lobby
    /// Each request time is recorded so we know if enough time has passed since the last request
    /// </summary>
    /// <returns>boolean</returns>
    private bool ValidateRequest(LobbyRequestType requestType) {
        if (!_requestLimits.ContainsKey(requestType)) {
            Debug.LogError($"Unknown request type: {requestType}");
            return false;
        }

        var now = DateTime.UtcNow;
        if (_lastRequestTime.TryGetValue(requestType, out var lastRequestTime)) {
            var timeSinceLastRequest = now - lastRequestTime;
            if (timeSinceLastRequest < _requestLimits[requestType]) {
                Debug.Log("Limiting Request: " + requestType);
                return false;
            }
        }
        
        _lastRequestTime[requestType] = now;
        return true;
    }


    /// <summary>
    /// Sign in the user to Unity Authentication Services
    /// </summary>
    /// <returns></returns>
    [ItemCanBeNull]
    public async Task<string> SignInUser() {
        var random = new System.Random();
        var randomNumber = random.Next();

        var options = new InitializationOptions();
        options.SetProfile("Player" + randomNumber.ToString());
        
        try {
            await UnityServices.InitializeAsync(options);
            if (!AuthenticationService.Instance.IsSignedIn) {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("Signed in user: " + AuthenticationService.Instance.PlayerId);
            }

            return AuthenticationService.Instance.PlayerId;
        }
        catch (Exception e) {
            Debug.LogException(e);
            return null;
        }
    }
    
    /// <summary>
    /// Create a lobby via the Lobby Service
    /// 2 requests per 6 seconds
    /// </summary>
    /// <returns>The created lobby</returns>
    public async Task<Lobby> CreateLobby(string lobbyName, int maxPlayers, string gameMode, Dictionary<string, string> data)
    {
        return await HandleRequest(LobbyRequestType.CreateLobby, async () =>
        {
            var playerData = SerializePlayerData(data);
            var player = new Player(AuthenticationService.Instance.PlayerId, null, playerData);

            var options = new CreateLobbyOptions()
            {
                IsPrivate = false,
                Player = player,
                Data = new Dictionary<string, DataObject>()
                {
                    {
                        "GameMode", new DataObject(
                            DataObject.VisibilityOptions.Public,
                            value: gameMode)
                    }
                }
            };

            try
            {
                var lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
                return lobby;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return null;
            }
        });
    }

    
    /// <summary>
    /// Join a lobby via the Lobby Service
    /// 2 requests per 6 seconds
    /// </summary>
    /// <returns>The joined lobby</returns>
    public async Task<Lobby> JoinLobby(Lobby lobby, Dictionary<string, string> data) {

        return await HandleRequest(LobbyRequestType.JoinLobby, async () => {

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
                return null;
            }
        });
    }
    
    /// <summary>
    /// Leave a lobby via the Lobby Service
    /// 5 requests per second
    /// </summary>
    public async Task<bool> LeaveLobby(string lobbyId) {

        return await HandleRequest(LobbyRequestType.LeaveLobby, async () => {
            try {
                var playerId = AuthenticationService.Instance.PlayerId;
                await LobbyService.Instance.RemovePlayerAsync(lobbyId, playerId);
                return true;
            }
            catch (LobbyServiceException e) {
                Debug.LogError(e);
                return false;
            }
        });
    }
    
    /// <summary>
    /// Delete a lobby via the Lobby Service
    /// </summary>
    public async Task<bool> DeleteLobby(string lobbyId) {
        
        try {
            await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
            return true;
        }
        catch (LobbyServiceException e) {
            Debug.LogError(e);
            return false;
        }
    }
    
    /// <summary>
    /// Get a lobby by its ID using the Lobby Service
    /// 1 request per second
    /// </summary>
    /// <returns>The requested lobby</returns>
    public async Task<Lobby> GetLobby(string lobbyId) {

        return await HandleRequest(LobbyRequestType.GetLobby, async () => {

            try {
                return await LobbyService.Instance.GetLobbyAsync(lobbyId);
            }
            catch (LobbyServiceException e) {
                Debug.LogError(e);
                return null;
            }
        });
    }

    /// <summary>
    /// Get a list of all lobbies via the Lobby Service
    /// 1 request per second
    /// </summary>
    /// <returns>List of lobbies</returns>
    public async Task<List<Lobby>> GetLobbiesList() {

        return await HandleRequest(LobbyRequestType.GetLobbiesList, async () => {
            try {
                var queryResponse = await Lobbies.Instance.QueryLobbiesAsync();
                return queryResponse.Results.ToList<Lobby>();
            }
            catch (LobbyServiceException e) {
                Debug.LogError(e);
                return null;
            }
        });
    }
    
    /// <summary>
    /// Get a list of currently joined lobbies
    /// 1 request per 30 seconds
    /// </summary>
    /// <returns>List of joined lobbies</returns>
    public async Task<List<string>> GetJoinedLobbies() {

        return await HandleRequest(LobbyRequestType.GetJoinedLobbies, async () => {

            try {
                return await LobbyService.Instance.GetJoinedLobbiesAsync();
            }
            catch (Exception e) {
                Debug.LogError(e);
                return null;
            }
        });
    }
    
    /// <summary>
    /// Update a looby's data via the Lobby Service
    /// 5 requests per 5 seconds
    /// </summary>
    /// <returns>The updated lobby</returns>
    public async Task<Lobby> UpdateLobbyData(Dictionary<string, DataObject> lobbyData, string lobbyId) {

        return await HandleRequest(LobbyRequestType.UpdateLobbyData, async () => {

            try {
                var options = new UpdateLobbyOptions() {
                    HostId = AuthenticationService.Instance.PlayerId,
                    Data = lobbyData
                };
                return await LobbyService.Instance.UpdateLobbyAsync(lobbyId, options);
            }
            catch (LobbyServiceException e) {
                Debug.LogError(e);
                return null;
            }
        });
    }
    
    /// <summary>
    /// Update a lobby player's data via the Lobby Service
    /// 5 requests per 5 seconds
    /// </summary>
    /// <returns>The updated lobby</returns>
    public async Task<Lobby> UpdateLobbyPlayerData(UpdatePlayerOptions options, string playerId, string lobbyId) {

        return await HandleRequest(LobbyRequestType.JoinLobby, async () => {

            try {
                var lobby = await LobbyService.Instance.UpdatePlayerAsync(lobbyId, playerId, options);
                return lobby;
            }
            catch (LobbyServiceException e) {
                Debug.LogError(e);
                return null;
            }
        });
    }

    public async Task<ILobbyEvents> SubscribeToLobbyEvents(string lobbyId, LobbyEventCallbacks callbacks) {
        
        try {
            return await Lobbies.Instance.SubscribeToLobbyEventsAsync(lobbyId, callbacks);
        }
        catch (Exception e) {
            Debug.LogError(e);
            return null;
        }
        
    }
    
    public async Task<bool> UnsubscribeToLobbyEvents(ILobbyEvents lobbyEvents) {
        try {
            await lobbyEvents.UnsubscribeAsync();
            return true;
        }
        catch (Exception e) {
            Debug.LogError(e);
            return false;
        }
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