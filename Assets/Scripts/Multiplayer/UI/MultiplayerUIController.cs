using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Multiplayer;
using Multiplayer.UI;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using WebSocketSharp;
using LobbyView = Multiplayer.UI.LobbyView;

public class MultiplayerUIController : MonoBehaviour {
    
    // UI Document
    [SerializeField] public UIDocument uiDocument;
    
    // Client Data
    public string _serverStatus;
    public string _serverIP;
    public string _serverPort;
    
    // Managers
    private static ViewManager _viewManager;
    private GameLobbyManager _gameLobbyManager;
    
    // Global State
    private static Global_State _globalState;
    public GameObject gameLobbyManagerObj;
    
    // Cancellation Token Source
    private CancellationTokenSource cancellationTokenSource;
    
    // Input System
    private Controls _controls;
    
    void Awake(){
#if DEDICATED_SERVER
        gameObject.SetActive(false);
        return;
#endif
        _viewManager = ViewManager.Instance;
        _globalState = GameObject.Find("Global State").GetComponent<Global_State>();
        _gameLobbyManager = gameLobbyManagerObj.GetComponent<GameLobbyManager>();
    }

    void Start(){
        Debug.Log("In UI Controller Start");
        _viewManager.Initialize(this);
        //playerIdLabel.text = "Player ID: " + gameLobbyManager.GetPlayerID();
    }
    
    void OnEnable(){
        _controls = new Controls();
        _controls.UI.Enable();
        _controls.UI.ESC.performed += OnEscape;
    }

    void OnDisable(){
        _controls.UI.ESC.performed -= OnEscape;
        _controls.UI.Disable();
    }

    private void Update() {
        // if (lobbyLoader != null) {
        //     if (timer >= 1) {
        //         RotateLoader();
        //         timer = 0;
        //     }
        //     else {
        //         timer += Time.deltaTime;
        //     }
        // }
    }

    public static void OnClickMultiplayerMenuBtn(Type type) {
        Debug.Log($"Type: {type}");

        if (type.IsSubclassOf(typeof(Modal))) {
            _viewManager.OpenModal(type);
        }
        else if(type.IsSubclassOf(typeof(View))) {
            _viewManager.ChangeView(type);
        } 
        else {
            Debug.LogError("Type is neither a View nor a Modal.");
        }
    }

    public static void OnClickMainMenuBtn() {
        _globalState.LoadScene("Main Menu");
    }
    
    public async Task CreateLobby(string lobbyName) {
        await _gameLobbyManager.CreateLobby(lobbyName);
        _viewManager.CloseModal(typeof(CreateLobbyModal));
        _viewManager.ChangeView(typeof(LobbyView));
    }

    public async Task<List<Player>> GetLobbyPlayerTableData(bool sendNewRequest) {
        if (!sendNewRequest) {
            return await _gameLobbyManager.RefreshLobbyPlayers();
        } 
        return await _gameLobbyManager.GetLobbyPlayers();
    }
    
    public async Task<List<Lobby>> GetLobbyTableData(bool sendNewRequest) {
        if (!sendNewRequest) {
            return await _gameLobbyManager.RefreshLobbyList();
        }
        return await _gameLobbyManager.GetLobbiesList();
    }

    public bool IsPlayerHost() {
        return _gameLobbyManager.IsPlayerHost();
    }

    public bool IsPlayerHost(string playerId) {
        return _gameLobbyManager.IsPlayerHost(playerId);
    }
    
    public async Task<bool> StartGame() {
        cancellationTokenSource = new CancellationTokenSource();
        var clientConnected = await StartClientAsLobbyHost(cancellationTokenSource.Token);
        return clientConnected;
    }
    
    public async Task<bool> JoinGame() {
        var clientConnected = await StartClientAsLobbyPlayer();
        return clientConnected;
    }
    
    public async Task LeaveLobby() {
        await _gameLobbyManager.LeaveLobby();
        _viewManager.UpdateView(typeof(MultiplayerMenuView));
        _viewManager.ChangeView(typeof(MultiplayerMenuView));
    }
    
    public async Task<bool> IsPlayerInLobby(Lobby lobby) {
        return await _gameLobbyManager.IsPlayerInLobby(lobby);
    }
    
    public bool IsPlayerInLobby() {
        return _gameLobbyManager.IsPlayerInLobby();
    }
    
   public async Task JoinLobby(Lobby lobby) {
       await _gameLobbyManager.JoinLobby(lobby);
       _viewManager.ChangeView(typeof(LobbyView));
   }
   
   private void OnEscape(InputAction.CallbackContext context) {
       if (_viewManager.CurrentModal != null) {
           _viewManager.CloseModal(typeof(ExitGameModal));
       }
       else {
           _viewManager.OpenModal(typeof(ExitGameModal));
       }
   }

   public void CloseModal(Type type) {
       _viewManager.CloseModal(type);
   }

   public void ReturnToMultiplayerMenu() {
       _viewManager.UpdateView(typeof(MultiplayerMenuView));
       _viewManager.ChangeView(typeof(MultiplayerMenuView));
   }

   public void DisconnectClient() {
       NetworkManager.Singleton.Shutdown();
       _viewManager.CloseModal(typeof(ExitGameModal));
       _viewManager.ChangeView(typeof(LobbyView));
   }
   
   // TODO - Ensure only lobby hosts can request a server allocation
   private async Task<bool> StartClientAsLobbyHost(CancellationToken cancellationToken) {

       try {
           WebServicesAPI webServicesAPI = new WebServicesAPI();
           await webServicesAPI.RequestAPIToken();
           var clientConnectionInfo = await GetClientConnectionInfo(cancellationToken, webServicesAPI);
           if (!clientConnectionInfo.IP.IsNullOrEmpty()) {
               _serverIP = clientConnectionInfo.IP;
               _serverPort = clientConnectionInfo.Port.ToString();
               _viewManager.RePaintView(typeof(LobbyView));
               await _gameLobbyManager.UpdateLobbyWithServerInfo(_serverStatus, clientConnectionInfo.IP, clientConnectionInfo.Port);
               NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(clientConnectionInfo.IP, (ushort)clientConnectionInfo.Port);
               // Send playerId of client to server in the connection approval payload 
               var playerData = new PlayerData { PlayerId = _gameLobbyManager.GetPlayerID() };
               using (var writer = new FastBufferWriter(128, Allocator.Temp)) {
                   writer.WriteNetworkSerializable(playerData);
                   NetworkManager.Singleton.NetworkConfig.ConnectionData = writer.ToArray();
               }
               NetworkManager.Singleton.StartClient();
               NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("PlayerStatusMessage",
                   OnPlayerStatusMessage);
               await _gameLobbyManager.UpdatePlayerDataWithClientId(NetworkManager.Singleton.LocalClientId);
               return true;
           }
       }
       catch (OperationCanceledException) {
           
       }
       return false;
   }

   private async Task<bool> StartClientAsLobbyPlayer() {
       
       var ip =  _gameLobbyManager?.GetLobbyData("ServerIP");
       var port = _gameLobbyManager?.GetLobbyData("Port");

       if (ip == null || port == null) return false;
       
       try {
           NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ip, (ushort)int.Parse(port));
           
           // Send playerId of client to server in the connection approval payload 
           var playerData = new PlayerData { PlayerId = _gameLobbyManager.GetPlayerID() };
           using (var writer = new FastBufferWriter(128, Allocator.Temp)) {
               writer.WriteNetworkSerializable(playerData);
               NetworkManager.Singleton.NetworkConfig.ConnectionData = writer.ToArray();
           }
           NetworkManager.Singleton.StartClient();
           NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("PlayerStatusMessage",
               OnPlayerStatusMessage);
           await _gameLobbyManager.UpdatePlayerDataWithClientId(NetworkManager.Singleton.LocalClientId);
           
           return true;
       }
       catch (OperationCanceledException) {
           
       }
       return false;
   }
   
   private async Task<ClientConnectionInfo> GetClientConnectionInfo(CancellationToken cancellationToken, WebServicesAPI webServicesAPI) {
            
       // Queue allocation request for a game server
       await webServicesAPI.QueueAllocationRequest();
       
       // Check for active machine
       _serverStatus = await webServicesAPI.GetMachineStatus();
       _viewManager.RePaintView(typeof(LobbyView));
       
       Debug.Log("Initial Machine Status: " + _serverStatus);
       
       // If no machine is found yet, keep polling until one is
       if (_serverStatus != "ONLINE") {
           var machines = await webServicesAPI.PollForMachine(60 * 5, cancellationToken);
           
           if (machines.Length == 0) {
               Debug.LogError("No machines found after polling.");
               return new ClientConnectionInfo { IP = "", Port = 0 };
           }
           
           var maxRetries = 60;
           var retryCount = 0;
           
           // Once a machine is found, poll for its status until it's online
           while (retryCount < maxRetries) {
               Debug.Log("Polling Machine Status");
               var newStatus = await webServicesAPI.GetMachineStatus();
               if (_serverStatus != newStatus) {
                   Debug.Log("Machine Status Change: " + newStatus);
                   _serverStatus = newStatus;
                   _viewManager.RePaintView(typeof(LobbyView));
                   if (_serverStatus == "ONLINE") {
                       break;
                   }
               }
               retryCount++;
               await Task.Delay(5000, cancellationToken);
           }
       }
       else {
           _viewManager.RePaintView(typeof(LobbyView));
       }
       
       // Now the machine is online, poll for the IP and Port of allocated game server
       var response = await webServicesAPI.PollForAllocation(60 * 5, cancellationToken);
       return response != null
           ? new ClientConnectionInfo { IP = response.ipv4, Port = response.gamePort }
           : new ClientConnectionInfo { IP = "", Port = 0 };

   }
   
    void RotateLoader() {
        // rotation += 360;
        // lobbyLoader.style.rotate =
        //     new StyleRotate(new UnityEngine.UIElements.Rotate(new Angle(rotation, AngleUnit.Degree)));
    }
    
    public async Task OnLobbyChanged(ILobbyChanges changes) {
        if (changes.PlayerLeft.Changed) {
            Debug.Log("Lobby Change - Player left!");
        }

        if (changes.PlayerJoined.Changed) {
            Debug.Log("Lobby Change - Player Joined!");
        }

        if (changes.Data.Added) {
            Debug.Log("Lobby Change - Data Added");
        }

        if (changes.PlayerData.Changed) {
            Debug.Log("Lobby Change - Player Data Added");
        }
        
        _gameLobbyManager.ApplyLobbyChanges(changes);
        _viewManager.UpdateView(typeof(LobbyView));
    }
    private async void OnPlayerStatusMessage(ulong clientId, FastBufferReader reader) {
        PlayerStatusMessage msg = new PlayerStatusMessage();
        reader.ReadNetworkSerializable<PlayerStatusMessage>(out msg);
        Debug.Log("Player Status Message: " + msg.PlayerId + " Connected: " + msg.IsConnected);
        await _gameLobbyManager.UpdatePlayerDataWithConnectionStatus(msg.IsConnected, msg.PlayerId.ToString());
    }
}
