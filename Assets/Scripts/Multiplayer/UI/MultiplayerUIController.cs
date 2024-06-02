using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Multiplayer;
using Multiplayer.UI;
using QFSW.QC;
using Unity.Netcode;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using LobbyView = Multiplayer.UI.LobbyView;

public class MultiplayerUIController : MonoBehaviour {
    
    // UI Document
    [SerializeField] public UIDocument uiDocument;
    
    // Lobby Manager
    public GameObject gameLobbyManagerObj;
    private GameLobbyManager _gameLobbyManager;
    
    // View Manager
    public GameObject gameViewManagerObj;
    private GameViewManager _gameViewManager;
    private ViewManager _viewManager;
    
    // Client Manager
    public GameObject clientManagerObj;
    private ClientManager _clientManager;
    public Client _client;
    
    // Global State
    private static Global_State _globalState;
    
    // Input System
    private Controls _controls;
    
    void Awake(){
#if DEDICATED_SERVER
        gameObject.SetActive(false);
        return;
#endif
        _globalState = GameObject.Find("Global State").GetComponent<Global_State>();
        
        _gameLobbyManager = gameLobbyManagerObj.GetComponent<GameLobbyManager>();
        _gameViewManager = gameViewManagerObj.GetComponent<GameViewManager>();
        _clientManager = clientManagerObj.GetComponent<ClientManager>();
    }

    void Start(){
        _viewManager = _gameViewManager.ViewManager;
        _viewManager.Initialize(this);
        _client = _clientManager.Client;
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

    
    public void OnClickMultiplayerMenuBtn(Type type) {
        
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
    
    private void OnEscape(InputAction.CallbackContext context) {
        if (_viewManager.CurrentModal != null) {
            _viewManager.CloseModal(typeof(ExitGameModal));
        }
        else {
            _viewManager.OpenModal(typeof(ExitGameModal));
        }
    }

    public void OnClickMainMenuBtn() {
        _globalState.LoadScene("Main Menu");
    }
    
    public async Task CreateLobby(string lobbyName) {
        await _gameLobbyManager.CreateLobby(lobbyName);
        _client.IsLobbyHost = true;
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
        var clientConnected = await _clientManager.Connect();
        _viewManager.ChangeView(typeof(GameView));
        return clientConnected;
    }
    
    public async Task<bool> JoinGame() {
        var clientConnected = await _clientManager.Connect();
        _viewManager.ChangeView(typeof(GameView));
        return clientConnected;
    }
    
    public async Task LeaveLobby() {
        if (_client.IsLobbyHost) {
            await _gameLobbyManager.DeleteLobby();
            //Delete allocation?
        }
        else {
            await _gameLobbyManager.LeaveLobby();
        }
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
       _client.IsLobbyHost = false;
        UpdatePlayersServerInfo();
       _viewManager.ChangeView(typeof(LobbyView));
   }
   
   public void CloseModal(Type type) {
       _viewManager.CloseModal(type);
   }

   public void ReturnToMultiplayerMenu() {
       _viewManager.ChangeView(typeof(MultiplayerMenuView));
   }

   /// <summary>
   /// Process player initiated disconnect 
   /// </summary>
   public async Task DisconnectClient() {
       
       if (_viewManager.CurrentModal.GetType() == typeof(ExitGameModal)) {
           if (_client.IsLobbyHost) {
               await DisconnectHost();
           }
           else {
               await _clientManager.Disconnect();
               _viewManager.CloseModal(typeof(ExitGameModal));
               _viewManager.RePaintView(typeof(LobbyView));
               _viewManager.ChangeView(typeof(LobbyView));
           }
       }
   }

   private async Task DisconnectHost() {
       Debug.Log("Host is sending disconnect message to server.");
       _clientManager.NotifyServerOfHostDisconnect();
       
       var maxRetries = 10;
       var retryCount = 0;
       
       while (retryCount < maxRetries) {
           if (_gameLobbyManager.LobbyPlayersDisconnected()) {
               break;
           }
           Debug.Log("Wait for lobby players to disconnect...");
           await Task.Delay(1000);
           retryCount++;
       }
       var canDisconnect = _gameLobbyManager.LobbyPlayersDisconnected();
       if (canDisconnect) {
           await _clientManager.Disconnect();
           _viewManager.CloseModal(typeof(ExitGameModal));
           _viewManager.RePaintView(typeof(LobbyView));
           _viewManager.ChangeView(typeof(LobbyView));
       }
       else {
           Debug.LogError("Players have not disconnected.");
       }
   }
   
    void RotateLoader() {
        // rotation += 360;
        // lobbyLoader.style.rotate =
        //     new StyleRotate(new UnityEngine.UIElements.Rotate(new Angle(rotation, AngleUnit.Degree)));
    }
    
    public void OnLobbyChanged(ILobbyChanges changes) {
        if (changes.PlayerLeft.Changed) {
            Debug.Log("Lobby Change - Player left!");
        }

        if (changes.PlayerJoined.Changed) {
            Debug.Log("Lobby Change - Player Joined!");
        }

        if (changes.Data.Added) {
            Debug.Log("Lobby Change - Lobby Data Added");
        }

        if (changes.PlayerData.Changed) {
            Debug.Log("Lobby Change - Player Data Changed");
        }

        if (changes.LobbyDeleted) {
            Debug.Log("Lobby Change - Lobby Deleted");
            if (!_client.IsLobbyHost) {
                _gameLobbyManager.InvalidateLobby();
                _viewManager.ChangeView(typeof(MultiplayerMenuView));
                return;
            }
        }
        
        _gameLobbyManager.ApplyLobbyChanges(changes);
        
        if (!_client.IsLobbyHost) {
            UpdatePlayersServerInfo();
        }
        else {
            _viewManager.RePaintView(typeof(LobbyView));
        }
    }

    /// <summary>
    /// Update the server info in the Lobby View for players (non lobby host)
    /// </summary>
    private void UpdatePlayersServerInfo() {
        
         if(_gameLobbyManager?.GetLobbyData("ServerIP") != null) {
             Debug.Log("In here1");
             _client.ServerIP = _gameLobbyManager?.GetLobbyData("ServerIP");
             _client.Port = _gameLobbyManager?.GetLobbyData("Port");
         }

         if (_gameLobbyManager?.GetLobbyData("MachineStatus") != null) {
             Debug.Log("In here2");
             _client.ServerStatus = _gameLobbyManager?.GetLobbyData("MachineStatus");
             
         }
         _viewManager.RePaintView(typeof(LobbyView));
    }

    public bool CanJoinGame() {
        return _gameLobbyManager.HostIsConnected();
    }

    public bool CanStartGame() {
        return !_gameLobbyManager.HostIsConnected();
    }
}
