using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Multiplayer;
using Multiplayer.UI;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using LobbyView = Multiplayer.UI.LobbyView;

public class MultiplayerUIController : MonoBehaviour {
    
    // UI Document
    [SerializeField] public UIDocument uiDocument;
    
    // Client
    public Client Client;
    
    // Managers
    private GameLobbyManager _gameLobbyManager;
    private static ViewManager _viewManager;
    private ClientManager _clientManager;
    
    // Global State
    private static GlobalState _globalState;
    
    // Input System
    private Controls _controls;
    
    // Views
    public List<View> Views;
    public GameView GameView;
    public LobbiesView LobbiesView;
    public LobbyView LobbyView;
    public static MultiplayerMenuView MultiplayerMenuView;
    
    // Modals
    public List<Modal> Modals;
    public CreateLobbyModal CreateLobbyModal;
    public ExitGameModal ExitGameModal;
    
    // Templates
    public VisualTreeAsset multiplayerMenuViewTmpl;
    public VisualTreeAsset lobbiesViewTmpl;
    public VisualTreeAsset lobbyViewTmpl;
    public VisualTreeAsset createLobbyModalTmpl;
    public VisualTreeAsset exitGameModalTmpl;
    public VisualTreeAsset gameViewTmpl;
    
    private void Awake(){
        #if DEDICATED_SERVER
            gameObject.SetActive(false);
            return;
        #endif
        
        InitViews();
        InitModals();
        
        _globalState = GameObject.Find("Global State").GetComponent<GlobalState>();
        _gameLobbyManager = GameLobbyManager.Instance;
        _viewManager = ViewManager.Instance;
    }

     private async void Start() {
         _clientManager = ClientManager.Instance;
         Client = _clientManager.Client;
         _viewManager.Initialize(Views, Modals, MultiplayerMenuView);
         Client.ID = await _gameLobbyManager.SignIn();
         _viewManager.RePaintView(MultiplayerMenuView);
     }
    
    void OnEnable(){
        _controls = new Controls();
        _controls.UI.Enable();
        _controls.UI.ESC.performed += OnEscape;
        
        MultiplayerMenuView.OpenCreateLobbyModal += (() => _viewManager.OpenModal(CreateLobbyModal));
        MultiplayerMenuView.ShowLobbyView += (() => _viewManager.ChangeView(LobbyView));
        MultiplayerMenuView.ShowLobbiesView += (() => _viewManager.ChangeView(LobbiesView));
        MultiplayerMenuView.LoadMainMenuScene += (() => _globalState.LoadScene("Main Menu"));

        LobbiesView.JoinLobby += (async (lobby) => await JoinLobby(lobby));

    }

    void OnDisable(){
        _controls.UI.ESC.performed -= OnEscape;
        _controls.UI.Disable();
    }

    private void Update() {
        if (_viewManager != null && _viewManager.CurrentModal != null) {
            _viewManager.CurrentModal.UpdateLoader();
        }
    }

    private void InitViews() {
        var root = uiDocument.rootVisualElement;
        var viewContainer = root.Q<VisualElement>("view-container");

        GameView = new GameView(root, this, gameViewTmpl);
        LobbiesView = new LobbiesView(viewContainer, this, lobbiesViewTmpl);
        LobbyView = new LobbyView(viewContainer, this, lobbyViewTmpl);
        MultiplayerMenuView = new MultiplayerMenuView(viewContainer, this, multiplayerMenuViewTmpl);

        Views = new List<View>() {
            MultiplayerMenuView,
            GameView,
            LobbiesView,
            LobbyView,
        };
    }

    private void InitModals() {
        var root = uiDocument.rootVisualElement;
        
        CreateLobbyModal = new CreateLobbyModal(root, this, createLobbyModalTmpl);
        ExitGameModal = new ExitGameModal(root, this, exitGameModalTmpl);

        Modals = new List<Modal>() {
            CreateLobbyModal,
            ExitGameModal
        };
    }
    
    public void OnCloseModal(Modal modal) {
        _viewManager.CloseModal(modal);
    }
    
    private void OnEscape(InputAction.CallbackContext context) {
        if (_viewManager.CurrentView == GameView) {
            if (_viewManager.CurrentModal == ExitGameModal) {
                _viewManager.CloseModal(ExitGameModal);
            }
            else {
                _viewManager.OpenModal(ExitGameModal);
            }
        }
    }
    
    public async Task CreateLobby(string lobbyName) {
        await _gameLobbyManager.CreateLobby(lobbyName);
        Client.IsLobbyHost = true;
        _viewManager.CloseModal(CreateLobbyModal);
        _viewManager.ChangeView(LobbyView);
    }

    public async Task<List<Player>> GetLobbyPlayerTableData(bool sendNewRequest) {
        if (!sendNewRequest) {
            return await _gameLobbyManager.GetLocalLobbyPlayers();
        } 
        return await _gameLobbyManager.GetLobbyPlayers();
    }
    
    public async Task<List<Lobby>> GetLobbyTableData(bool sendNewRequest) {
        if (!sendNewRequest) {
            return await _gameLobbyManager.GetLocalLobbiesList();
        }
        return await _gameLobbyManager.GetLobbiesList();
    }

    public bool IsPlayerHost() {
        return _gameLobbyManager.IsPlayerLobbyHost();
    }

    public bool IsPlayerHost(string playerId) {
        return _gameLobbyManager.IsPlayerLobbyHost(playerId);
    }
    
    public async Task<bool> StartGame() {
        var clientConnected = await _clientManager.Connect();
        _viewManager.ChangeView(GameView);
        return clientConnected;
    }
    
    public async Task<bool> JoinGame() {
        var clientConnected = await _clientManager.Connect();
        _viewManager.ChangeView(GameView);
        return clientConnected;
    }
    
    public async Task LeaveLobby() {
        if (Client.IsLobbyHost) {
            await _gameLobbyManager.DeleteLobby();
            //Delete allocation?
        }
        else {
            await _gameLobbyManager.LeaveLobby();
        }
        _viewManager.ChangeView(MultiplayerMenuView);
    }
    
    public async Task<bool> IsPlayerInLobby(Lobby lobby) {
        return await _gameLobbyManager.IsPlayerInLobby(lobby);
    }
    
    public bool IsPlayerInLobby() {
        return _gameLobbyManager.IsPlayerInLobby();
    }
    
   private async Task JoinLobby(Lobby lobby) {
       await _gameLobbyManager.JoinLobby(lobby);
       Client.IsLobbyHost = false;
        UpdatePlayersServerInfo();
       _viewManager.ChangeView(LobbyView);
   }
   
   public static void ReturnToMultiplayerMenu() {
       _viewManager.ChangeView(MultiplayerMenuView);
   }

   /// <summary>
   /// Process player initiated disconnect 
   /// </summary>
   public async Task DisconnectClient() {
       
       if (_viewManager.CurrentModal.GetType() == typeof(ExitGameModal)) {
           if (Client.IsLobbyHost) {
               await DisconnectHost();
           }
           else {
               await _clientManager.Disconnect();
               _viewManager.CloseModal(ExitGameModal);
               _viewManager.RePaintView(LobbyView);
               _viewManager.ChangeView(LobbyView);
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
           _viewManager.CloseModal(ExitGameModal);
           _viewManager.RePaintView(LobbyView);
           _viewManager.ChangeView(LobbyView);
       }
       else {
           Debug.LogError("Players have not disconnected.");
       }
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
            if (!Client.IsLobbyHost) {
                _gameLobbyManager.InvalidateLobby();
                _viewManager.ChangeView(MultiplayerMenuView);
                return;
            }
        }
        
        _gameLobbyManager.ApplyLobbyChanges(changes);
        
        if (!Client.IsLobbyHost) {
            UpdatePlayersServerInfo();
        }
        else {
            _viewManager.RePaintView(LobbyView);
        }
    }

    /// <summary>
    /// Update the server info in the Lobby View for players (non lobby host)
    /// </summary>
    private void UpdatePlayersServerInfo() {
        
         if(_gameLobbyManager?.GetLobbyData("ServerIP") != null) {
             Client.ServerIP = _gameLobbyManager?.GetLobbyData("ServerIP");
             Client.Port = _gameLobbyManager?.GetLobbyData("Port");
         }

         if (_gameLobbyManager?.GetLobbyData("MachineStatus") != null) {
             Client.ServerStatus = _gameLobbyManager?.GetLobbyData("MachineStatus");
             
         }
         _viewManager.RePaintView(LobbyView);
    }

    public bool CanJoinGame() {
        return _gameLobbyManager.HostIsConnected();
    }

    public bool CanStartGame() {
        return !_gameLobbyManager.HostIsConnected();
    }
}
