using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Global.Game_Modes;
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
    private LobbyPlayerView _lobbyPlayerView;
    private LobbyHostView _lobbyHostView;
    private static MultiplayerMenuView _multiplayerMenuView;
    
    // Modals
    private List<Modal> _modals;
    private CreateLobbyModal _createLobbyModal;
    private ExitGameModal _exitGameModal;
    private MessageModal _messageModal;
    
    // UXML Templates
    // Views
    public VisualTreeAsset multiplayerMenuViewTmpl;
    public VisualTreeAsset lobbiesViewTmpl;
    public VisualTreeAsset lobbyHostViewTmpl;
    public VisualTreeAsset lobbyPlayerViewTmpl;
    public VisualTreeAsset gameViewTmpl;
    
    // Modals
    public VisualTreeAsset createLobbyModalTmpl;
    public VisualTreeAsset exitGameModalTmpl;
    
    // Message Modals
   public VisualTreeAsset messageModalSignInConnectingTmpl;
   public VisualTreeAsset messageModalSignInFailedTmpl;
   public VisualTreeAsset messageModalLobbyFailedTmpl;
   public VisualTreeAsset messageModalServerStartPendingTmpl;
   

    
    //Gamemode
    private GameMode _currentGameMode;
    
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
         _viewManager.Initialize(_views, _modals, _multiplayerMenuView);
         
         // Sign-in to Unity Services
         await LobbySignIn();
         
         // Client Manager Events
         _clientManager.RePaintLobbyView += () => _viewManager.RePaintView(_lobbyHostView); // TODO: Remove?
         _clientManager.HostDisconnection += () => {
             _viewManager.RePaintView(_lobbyPlayerView);
             _viewManager.ChangeView(_lobbyPlayerView);
         };
         _clientManager.UpdateServerData += (client) => {
             _messageModal.UpdateBodyLabel(client.ServerStatus);
             if (client.ServerStatus == MachineStatus.Online) {
                 if (_viewManager.CurrentModal == _messageModal) {
                     _viewManager.CloseModal();
                     _lobbyHostView.RePaint();
                 }
             }
         };
     }
    
    void OnEnable(){
        
        // Input Events
        _controls = new Controls();
        _controls.UI.Enable();
        _controls.UI.ESC.performed += OnEscape;
        
        // Multiplayer Main Menu View Events
        _multiplayerMenuView.OpenCreateLobbyModal += (() => _viewManager.OpenModal(_createLobbyModal));
        _multiplayerMenuView.ShowLobbyView += (() => {
            _viewManager.ChangeView(IsPlayerHost() ? _lobbyHostView : _lobbyPlayerView);
        });
        _multiplayerMenuView.ShowLobbiesView += (() => _viewManager.ChangeView(_lobbiesView));
        _multiplayerMenuView.LoadMainMenuScene += (() => _globalState.LoadScene("Main Menu"));
        _multiplayerMenuView.IsPlayerInLobby += IsPlayerInLobby;
        _multiplayerMenuView.IsPlayerSignedIn += (() => _gameLobbyManager.IsPlayerSignedIn());

        // Lobbies View Events
        _lobbiesView.JoinLobby += (async (lobby) => await JoinLobby(lobby));
        _lobbiesView.GetLobbyTableData += (async (sendNewRequest) => await GetLobbyTableData(sendNewRequest));
        _lobbiesView.IsPlayerInLobby += (async (lobby) => await IsPlayerInLobby(lobby));
        
        // Lobby Host View
        _lobbyHostView.CanStartGame += CanStartGame;
        _lobbyHostView.CanStartServer += CanStartServer;
        _lobbyHostView.StartGame += (async () => await StartGame());
        _lobbyHostView.StartServer += (async () => await StartServer());
        _lobbyHostView.LeaveLobby += (async () => await LeaveLobby());
        _lobbyHostView.GetLobbyPlayerTableData += (async (sendNewRequest) => await GetLobbyPlayerTableData(sendNewRequest));
        _lobbyHostView.GetLobbyPlayerId += () => _clientManager.Client.ID;
        _lobbyHostView.IsPlayerHost += (playerId) => IsPlayerHost(playerId);
        _lobbyHostView.GetLobbyGameMode += () => _gameLobbyManager.GetLobbyGameMode();
        
        // Lobby Player View
        _lobbyPlayerView.ReadyUp += (async () => await ReadyUp());
        _lobbyPlayerView.LeaveLobby += (async () => await LeaveLobby());
        _lobbyPlayerView.GetLobbyPlayerTableData += (async (sendNewRequest) => await GetLobbyPlayerTableData(sendNewRequest));
        _lobbyPlayerView.GetLobbyPlayerId += () => _clientManager.Client.ID;
        _lobbyPlayerView.IsPlayerHost += (playerId) => IsPlayerHost(playerId);
        _lobbyPlayerView.GetLobbyGameMode += () => _gameLobbyManager.GetLobbyGameMode();
        
        // Create Lobby Modal Events
        _createLobbyModal.CreateLobby += (async (lobbyName, maxPlayers, gameMode) => await CreateLobby(lobbyName, maxPlayers, gameMode));
        _createLobbyModal.CloseModal += (modal) => _viewManager.CloseModal(modal);
        
        // Exit Game Modal Events
        _exitGameModal.CloseModal += (modal) => _viewManager.CloseModal(modal);
        _exitGameModal.DisconnectClient += (async () => await DisconnectClient());


        _gameLobbyManager.OnShowMessage += ShowLobbyErrorMessage;
    }

    void OnDisable(){
        _controls.UI.ESC.performed -= OnEscape;
        _controls.UI.Disable();
    }
    
    /// <summary>
    /// Initialize all Views and store them in the Views list
    /// </summary>
    private void InitViews() {
        var root = uiDocument.rootVisualElement;
        var viewContainer = root.Q<VisualElement>("view-container");

        _gameView = new GameView(root, gameViewTmpl);
        _lobbiesView = new LobbiesView(viewContainer, lobbiesViewTmpl);
        _lobbyHostView = new LobbyHostView(viewContainer, lobbyHostViewTmpl);
        _lobbyPlayerView = new LobbyPlayerView(viewContainer, lobbyPlayerViewTmpl);
        _multiplayerMenuView = new MultiplayerMenuView(viewContainer, multiplayerMenuViewTmpl);

        _views = new List<View>() {
            _multiplayerMenuView,
            _gameView,
            _lobbiesView,
            _lobbyPlayerView,
            _lobbyHostView
        };
    }

    /// <summary>
    /// Initialize all Modals and store them in the Modals list
    /// </summary>
    private void InitModals() {
        var root = uiDocument.rootVisualElement;
        
        _createLobbyModal = new CreateLobbyModal(root, createLobbyModalTmpl);
        _exitGameModal = new ExitGameModal(root, exitGameModalTmpl);
        _messageModal = new MessageModal(root, messageModalSignInConnectingTmpl);

        _modals = new List<Modal>() {
            _createLobbyModal,
            _exitGameModal,
            _messageModal
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

    private void ShowLobbyErrorMessage() {
        _viewManager.OpenModal(_messageModal, messageModalLobbyFailedTmpl);
    }

    /// <summary>
    /// Sign-in to Unity Services to access the Lobby service
    /// </summary>
    private async Task LobbySignIn() {
          
        _viewManager.OpenModal(_messageModal);
         
        var clientId = await _gameLobbyManager.SignIn();
        
        if (clientId != null) {
            _clientManager.Client.ID = clientId;
            await Task.Delay(500);
            _viewManager.CloseModal();
            var playerIdLabel = uiDocument.rootVisualElement.Q<Label>("player-id");
            _multiplayerMenuView.DisplayPlayerId(_clientManager.Client.ID, playerIdLabel);
        }
        else {
            _viewManager.ChangeOpenModalTemplate(messageModalSignInFailedTmpl);
        }
        _viewManager.CurrentView.Update();
    }

    private LobbyView GetLobbyView() {
        return IsPlayerHost() ? _lobbyHostView : _lobbyPlayerView;
    }
    
    /// <summary>
    /// Create a new lobby as the host
    /// Show the lobby view
    /// </summary>
    /// <returns>boolean</returns>
    private async Task CreateLobby(string lobbyName, uint maxPlayers, string gameMode) {
        var success = await _gameLobbyManager.CreateLobby(lobbyName, (int)maxPlayers, gameMode);
        if (!success) return;
        _clientManager.Client.IsLobbyHost = true;
        _viewManager.CloseModal(_createLobbyModal);
        _viewManager.ChangeView(_lobbyHostView);
    }
    
    private async Task JoinLobby(Lobby lobby) {
        var success = await _gameLobbyManager.JoinLobby(lobby);
        if (!success) return;
        _clientManager.Client.IsLobbyHost = false;
        UpdatePlayersServerInfo();
        _viewManager.ChangeView(_lobbyPlayerView);
    }
    
    private async Task LeaveLobby() {
        var success = false;
        if (_clientManager.Client.IsLobbyHost) {
            success = await _gameLobbyManager.DeleteLobby();
            if (!success) return;
            if (_gameLobbyManager.currentServerProvisionState != ServerProvisionState.Idle &&
                _gameLobbyManager.currentServerProvisionState != ServerProvisionState.Failed) {
                _clientManager.StopServer();
                _gameLobbyManager.currentServerProvisionState = ServerProvisionState.Idle;
            }
        }
        else {
            success = await _gameLobbyManager.LeaveLobby();
        }

        if (!success) return;
        _viewManager.ChangeView(_multiplayerMenuView);
        
        if(_clientManager.Client.IsLobbyHost) _lobbyHostView.Reset();
        else _lobbyPlayerView.Reset();
    }
    
    private async Task StartServer() {
        _gameLobbyManager.currentServerProvisionState = ServerProvisionState.InProgress;
        _viewManager.OpenModal(_messageModal, messageModalServerStartPendingTmpl);
        await _clientManager.StartServer();
    }
    
    private async Task StartGame() {
        await _gameLobbyManager.UpdateLobbyWithGameStart(true);
        _gameLobbyManager.gameStarted = true;
         _clientManager.StartClient();
         GlobalState.GameModeManager.ChangeGameMode(_gameLobbyManager.GetLobbyGameMode());
        _viewManager.ChangeView(_gameView);
    }
    
    private void JoinGame() {
        _clientManager.StartClient();
        GlobalState.GameModeManager.ChangeGameMode(_gameLobbyManager.GetLobbyGameMode());
        _viewManager.ChangeView(_gameView);
    }

    private async Task<List<Player>> GetLobbyPlayerTableData(bool sendNewRequest) {
        if (!sendNewRequest) {
            return await _gameLobbyManager.GetLocalLobbyPlayers();
        } 
        var lobbyPlayers = await _gameLobbyManager.GetLobbyPlayers();
        if (lobbyPlayers != null) return lobbyPlayers;
        return null;
    }
    
    private async Task<List<Lobby>> GetLobbyTableData(bool sendNewRequest) {
        if (!sendNewRequest) {
            return await _gameLobbyManager.GetLocalLobbiesList();
        }
        return await _gameLobbyManager.GetLobbiesList();
    }
    
    /// <summary>
    /// Handling the removal of a lobby player when the host leaves
    /// </summary>
    private void RemoveLobbyPlayer() {
        _gameLobbyManager.InvalidateLobby();
        _clientManager.ResetClient();
        _lobbyPlayerView.Reset();
        _viewManager.ChangeView(_multiplayerMenuView);
    }

    private bool IsPlayerHost() {
        return _gameLobbyManager.IsPlayerLobbyHost();
    }

    private bool IsPlayerHost(string playerId) {
        return _gameLobbyManager.IsPlayerLobbyHost(playerId);
    }
    
    private async Task<bool> IsPlayerInLobby(Lobby lobby) {
        return await _gameLobbyManager.IsPlayerInLobby(lobby);
    }
    
    public bool IsPlayerInLobby() {
        return _gameLobbyManager.IsPlayerInLobby();
    }
    
   public static void ReturnToMultiplayerMenu() {
       _viewManager.ChangeView(_multiplayerMenuView);
   }

   /// <summary>
   /// Process player initiated disconnect 
   /// </summary>
   private async Task DisconnectClient() {
       
       if (_viewManager.CurrentModal == _exitGameModal) {
           if (_clientManager.Client.IsLobbyHost) {
               await DisconnectHost();
               await _gameLobbyManager.UpdateLobbyWithGameStart(false);
           }
           else {
               await _clientManager.Disconnect();
               _viewManager.CloseModal(_exitGameModal);
               _viewManager.RePaintView(_lobbyPlayerView);
               _viewManager.ChangeView(_lobbyPlayerView);
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
           _viewManager.RePaintView(_lobbyHostView);
           _viewManager.ChangeView(_lobbyHostView);
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
            if (!_clientManager.Client.IsLobbyHost) {
                RemoveLobbyPlayer();
                return;
            }
        }

        if (changes.Data.Changed) {
            try {
                var gameStarted = bool.Parse(changes.Data.Value["StartGame"].Value.Value);
                if (gameStarted && !IsPlayerHost()) {
                    JoinGame();
                }
            }
            catch (Exception e) {
                Debug.LogError(e);
            }
        }
        
        _gameLobbyManager.ApplyLobbyChanges(changes);
        
        if (!_clientManager.Client.IsLobbyHost) {
            UpdatePlayersServerInfo();
        }
        else {
            _viewManager.RePaintView(_lobbyHostView);
        }
    }

    /// <summary>
    /// Update the server info in the Lobby View for players (not the host)
    /// </summary>
    private void UpdatePlayersServerInfo() {

        if (_gameLobbyManager?.GetLobbyData("ServerIP") == null ||
            _gameLobbyManager?.GetLobbyData("MachineStatus") == null) return;
        
         _clientManager.Client.ServerIP = _gameLobbyManager?.GetLobbyData("ServerIP");
         _clientManager.Client.Port = _gameLobbyManager?.GetLobbyData("Port");
         _clientManager.Client.ServerStatus = _gameLobbyManager?.GetLobbyData("MachineStatus");
         
         _viewManager.RePaintView(_lobbyPlayerView);
    }

    private bool CanJoinGame() {
        return _gameLobbyManager.HostIsConnected();
    }

    /// <summary>
    /// Check if the host can start a game
    /// A host can start a game if...
    /// A server has been provisioned
    /// The minimum required count of players are in the lobby
    /// All players are ready
    /// </summary>
    /// <returns>boolean</returns>
    private bool CanStartGame() {

        var lobbyGameModeName = _gameLobbyManager.GetLobbyGameMode();
        var selectedGameMode = GlobalState.GameModeManager.GetGameMode(lobbyGameModeName);

        var serverIsProvisioned = _gameLobbyManager.currentServerProvisionState == ServerProvisionState.Provisioned;
        var allPlayersReady = _gameLobbyManager.PlayersReady();
        var lobbyHasRequiredPlayers = _gameLobbyManager.GetLobbyPlayerCount() >= selectedGameMode.MinimumRequiredPlayers;

        // Set the lobby status for the host
        if(!serverIsProvisioned){
            _lobbyHostView.SetStatus(LobbyHostView.HostLobbyStatus.StartServer);
        }
        else if(!lobbyHasRequiredPlayers) {
            _lobbyHostView.SetStatus(LobbyHostView.HostLobbyStatus.WaitForPlayers);
        }
        else if (!allPlayersReady) {
            _lobbyHostView.SetStatus(LobbyHostView.HostLobbyStatus.WaitForPlayersReady);
        }
        else {
            _lobbyHostView.SetStatus(LobbyHostView.HostLobbyStatus.StartGame);
        }
 
        return serverIsProvisioned && lobbyHasRequiredPlayers && allPlayersReady;
    }

    /// <summary>
    /// Check if the host can start a server
    /// A host can start a server if there isn't already server connection info stored (server hasn't been provisioned)
    /// </summary>
    /// <returns>boolean</returns>
    private bool CanStartServer() {
        return _gameLobbyManager.currentServerProvisionState == ServerProvisionState.Idle;
    }
    
    private async Task ReadyUp() {
        await _gameLobbyManager.UpdatePlayerIsReadyData();
        _viewManager.CurrentView.RePaint();
    }
}
