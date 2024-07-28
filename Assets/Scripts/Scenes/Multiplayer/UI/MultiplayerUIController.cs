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
    private List<View> _views;
    private GameView _gameView;
    private LobbiesView _lobbiesView;
    private LobbyView _lobbyView;
    private static MultiplayerMenuView _multiplayerMenuView;
    
    // Modals
    private List<Modal> _modals;
    private CreateLobbyModal _createLobbyModal;
    private ExitGameModal _exitGameModal;
    
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
         _viewManager.Initialize(_views, _modals, _multiplayerMenuView);
         Client.ID = await _gameLobbyManager.SignIn();
         
         var playerIdLabel = uiDocument.rootVisualElement.Q<Label>("player-id");
         _multiplayerMenuView.DisplayPlayerId(Client.ID, playerIdLabel);
         
         // Client Manager Events
         _clientManager.UpdateServerDataInLobbyView += (client) => {
             _lobbyView.UpdateServerInfoTable(client);
             _viewManager.RePaintView(_lobbyView);
         };
         _clientManager.RePaintLobbyView += () => _viewManager.RePaintView(_lobbyView);
         _clientManager.HostDisconnection += () => {
             _viewManager.RePaintView(_lobbyView);
             _viewManager.ChangeView(_lobbyView);
         };
     }
    
    void OnEnable(){
        
        // Input Events
        _controls = new Controls();
        _controls.UI.Enable();
        _controls.UI.ESC.performed += OnEscape;
        
        // Multiplayer Main Menu View Events
        _multiplayerMenuView.OpenCreateLobbyModal += (() => _viewManager.OpenModal(_createLobbyModal));
        _multiplayerMenuView.ShowLobbyView += (() => _viewManager.ChangeView(_lobbyView));
        _multiplayerMenuView.ShowLobbiesView += (() => _viewManager.ChangeView(_lobbiesView));
        _multiplayerMenuView.LoadMainMenuScene += (() => _globalState.LoadScene("Main Menu"));
        _multiplayerMenuView.IsThisPlayerInLobby += IsPlayerInLobby;

        // Lobbies View Events
        _lobbiesView.JoinLobby += (async (lobby) => await JoinLobby(lobby));
        _lobbiesView.GetLobbyTableData += (async (sendNewRequest) => await GetLobbyTableData(sendNewRequest));
        _lobbiesView.IsPlayerInLobby += (async (lobby) => await IsPlayerInLobby(lobby));

        // Lobby View Events
        _lobbyView.LeaveLobby += (async () => await LeaveLobby());
        _lobbyView.StartGame += (async () => await StartGame());
        _lobbyView.JoinGame += (async () => await JoinGame());
        _lobbyView.IsThisPlayerHost += IsPlayerHost;
        _lobbyView.IsPlayerHost += IsPlayerHost;
        _lobbyView.CanStartGame += CanStartGame;
        _lobbyView.CanJoinGame += CanJoinGame;
        _lobbyView.GetLobbyPlayerTableData += (async (sendNewRequest) => await GetLobbyPlayerTableData(sendNewRequest));

        // Create Lobby Modal Events
        _createLobbyModal.CreateLobby += (async (lobbyName) => await CreateLobby(lobbyName));
        _createLobbyModal.CloseModal += (modal) => _viewManager.CloseModal(modal);

        // Exit Game Modal Events
        _exitGameModal.CloseModal += (modal) => _viewManager.CloseModal(modal);
        _exitGameModal.DisconnectClient += (async () => await DisconnectClient());
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

    /// <summary>
    /// Initialize all Views and store them in the Views list
    /// </summary>
    private void InitViews() {
        var root = uiDocument.rootVisualElement;
        var viewContainer = root.Q<VisualElement>("view-container");

        _gameView = new GameView(root, gameViewTmpl);
        _lobbiesView = new LobbiesView(viewContainer, lobbiesViewTmpl);
        _lobbyView = new LobbyView(viewContainer, lobbyViewTmpl);
        _multiplayerMenuView = new MultiplayerMenuView(viewContainer, multiplayerMenuViewTmpl);

        _views = new List<View>() {
            _multiplayerMenuView,
            _gameView,
            _lobbiesView,
            _lobbyView,
        };
    }

    /// <summary>
    /// Initialize all Modals and store them in the Modals list
    /// </summary>
    private void InitModals() {
        var root = uiDocument.rootVisualElement;
        
        _createLobbyModal = new CreateLobbyModal(root, createLobbyModalTmpl);
        _exitGameModal = new ExitGameModal(root, exitGameModalTmpl);

        _modals = new List<Modal>() {
            _createLobbyModal,
            _exitGameModal
        };
    }
    
    private void OnEscape(InputAction.CallbackContext context) {
        if (_viewManager.CurrentView == _gameView) {
            if (_viewManager.CurrentModal == _exitGameModal) {
                _viewManager.CloseModal(_exitGameModal);
            }
            else {
                _viewManager.OpenModal(_exitGameModal);
            }
        }
    }
    
    private async Task CreateLobby(string lobbyName) {
        await _gameLobbyManager.CreateLobby(lobbyName);
        Client.IsLobbyHost = true;
        _viewManager.CloseModal(_createLobbyModal);
        _viewManager.ChangeView(_lobbyView);
    }

    private async Task<List<Player>> GetLobbyPlayerTableData(bool sendNewRequest) {
        if (!sendNewRequest) {
            return await _gameLobbyManager.GetLocalLobbyPlayers();
        } 
        return await _gameLobbyManager.GetLobbyPlayers();
    }
    
    private async Task<List<Lobby>> GetLobbyTableData(bool sendNewRequest) {
        if (!sendNewRequest) {
            return await _gameLobbyManager.GetLocalLobbiesList();
        }
        return await _gameLobbyManager.GetLobbiesList();
    }

    private bool IsPlayerHost() {
        return _gameLobbyManager.IsPlayerLobbyHost();
    }

    private bool IsPlayerHost(string playerId) {
        return _gameLobbyManager.IsPlayerLobbyHost(playerId);
    }
    
    private async Task<bool> StartGame() {
        _gameLobbyManager.gameStarted = true;
        var clientConnected = await _clientManager.Connect();
        _viewManager.ChangeView(_gameView);
        return clientConnected;
    }
    
    private async Task<bool> JoinGame() {
        var clientConnected = await _clientManager.Connect();
        _viewManager.ChangeView(_gameView);
        return clientConnected;
    }
    
    private async Task LeaveLobby() {
        if (Client.IsLobbyHost) {
            await _gameLobbyManager.DeleteLobby();
            //Delete allocation?
        }
        else {
            await _gameLobbyManager.LeaveLobby();
        }
        _viewManager.ChangeView(_multiplayerMenuView);
    }
    
    private async Task<bool> IsPlayerInLobby(Lobby lobby) {
        return await _gameLobbyManager.IsPlayerInLobby(lobby);
    }
    
    public bool IsPlayerInLobby() {
        return _gameLobbyManager.IsPlayerInLobby();
    }
    
   private async Task JoinLobby(Lobby lobby) {
       await _gameLobbyManager.JoinLobby(lobby);
       Client.IsLobbyHost = false;
        UpdatePlayersServerInfo();
       _viewManager.ChangeView(_lobbyView);
   }
   
   public static void ReturnToMultiplayerMenu() {
       _viewManager.ChangeView(_multiplayerMenuView);
   }

   /// <summary>
   /// Process player initiated disconnect 
   /// </summary>
   private async Task DisconnectClient() {
       
       if (_viewManager.CurrentModal == _exitGameModal) {
           if (Client.IsLobbyHost) {
               await DisconnectHost();
           }
           else {
               await _clientManager.Disconnect();
               _viewManager.CloseModal(_exitGameModal);
               _viewManager.RePaintView(_lobbyView);
               _viewManager.ChangeView(_lobbyView);
           }
       }
   }

   private async Task DisconnectHost() {
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
           _gameLobbyManager.gameStarted = false;
           _viewManager.CloseModal(_exitGameModal);
           _viewManager.RePaintView(_lobbyView);
           _viewManager.ChangeView(_lobbyView);
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
                _viewManager.ChangeView(_multiplayerMenuView);
                return;
            }
        }
        
        _gameLobbyManager.ApplyLobbyChanges(changes);
        
        if (!Client.IsLobbyHost) {
            UpdatePlayersServerInfo();
        }
        else {
            _viewManager.RePaintView(_lobbyView);
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
         _lobbyView.UpdateServerInfoTable(Client);
         _viewManager.RePaintView(_lobbyView);
    }

    private bool CanJoinGame() {
        return _gameLobbyManager.HostIsConnected();
    }

    /// <summary>
    /// Check if the host can start a game
    /// A host can start a game if they are not already connected to the server and a game is not currently in progress
    /// </summary>
    /// <returns>boolean</returns>
    private bool CanStartGame() {
        return !_gameLobbyManager.HostIsConnected() && !_gameLobbyManager.gameStarted;
    }
}
