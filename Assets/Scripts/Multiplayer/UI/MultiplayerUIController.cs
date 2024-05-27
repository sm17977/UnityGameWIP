using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Multiplayer;
using QFSW.QC;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using WebSocketSharp;

public class MultiplayerUIController : MonoBehaviour
{
    // UI Document
    [SerializeField] private UIDocument uiDocument;

    // Visual Elements
    
    // General
    private VisualElement _currentView;
    private Button _backToMultiplayerMenuBtn;
    private VisualElement _backToMultiplayerMenuBtnContainer;

    // Menu
    VisualElement mainContainer;
    VisualElement multiplayerMenuContainer;
    List<Button> menuBtns;
    Label playerIdLabel;
    private VisualElement _createLobbyMenuBtnContainer;
    private VisualElement _currentLobbyMenuBtnContainer;
    
    // Current Lobby
    private VisualElement _currentLobbyContainer;
    private VisualElement _currentLobbyTable;
    private Button _currentLobbyStartGameBtn;
    private Button _currentLobbyJoinGameBtn;
    private Button _currentLobbyleaveBtn;
    private string _serverStatus = "N/A";
    private string _serverIP;
    private string _serverPort;
    private string _playerConnectionStatus = "Not Connected";
    
    // Server Info 
    private VisualElement _serverInfoTable;
    private Label _serverStatusLabel;
    private Label _serverIPLabel;
    private Label _serverPortLabel;
    
    // Lobbies List
    VisualElement listLobbiesContainer;
    VisualElement listLobbiesTable;
    
    // Create Lobby Modal
    Button createLobbyBtn;
    Button cancelLobbyBtn;
    VisualElement lobbyModalContainer;
    TextField lobbyNameInput;
    private Label lobbyStatusLabel;
    
    // Game Exit Modal
    private VisualElement _gameExitModal;
    private Button _confirmGameExitBtn;
    private Button _cancelGameExitBtn;
    
    // Loader
    VisualElement lobbyLoader;
    private float rotation = 0;
    private float timer = 0;

    // Input System
    private Controls controls;

    // Global State
    private Global_State globalState;
    public GameObject gameLobbyManagerObj;
    private GameLobbyManager gameLobbyManager;
    
    // Cancellation Token Source
    private CancellationTokenSource cancellationTokenSource;



    void Awake(){
#if DEDICATED_SERVER
        gameObject.SetActive(false);
        return;
#endif
        globalState = GameObject.Find("Global State").GetComponent<Global_State>();
        gameLobbyManager = gameLobbyManagerObj.GetComponent<GameLobbyManager>();

        // Multiplayer Menu Elements 
        playerIdLabel = uiDocument.rootVisualElement.Q<Label>("player-id");
        mainContainer = uiDocument.rootVisualElement.Q<VisualElement>("main-container");
        multiplayerMenuContainer = uiDocument.rootVisualElement.Q<VisualElement>("menu-container");
        menuBtns = uiDocument.rootVisualElement.Query<Button>("btn").ToList();
        _createLobbyMenuBtnContainer = uiDocument.rootVisualElement.Q<VisualElement>("create-lobby-btn-container");
        _currentLobbyMenuBtnContainer = uiDocument.rootVisualElement.Q<VisualElement>("current-lobby-btn-container");
        
        // Current Lobby 
        _currentLobbyContainer = uiDocument.rootVisualElement.Q<VisualElement>("current-lobby-container");
        _currentLobbyTable = uiDocument.rootVisualElement.Q<VisualElement>("current-lobby-table-body");
        _currentLobbyleaveBtn = uiDocument.rootVisualElement.Q<Button>("leave-lobby-btn");
        _serverStatusLabel = uiDocument.rootVisualElement.Q<Label>("server-status-label");
        _serverIPLabel = uiDocument.rootVisualElement.Q<Label>("server-ip-label");
        _serverPortLabel = uiDocument.rootVisualElement.Q<Label>("server-port-label");
        _currentLobbyStartGameBtn = uiDocument.rootVisualElement.Q<Button>("start-game-btn");
        _currentLobbyStartGameBtn.RegisterCallback<ClickEvent>(async evt => await StartGame());
        _currentLobbyJoinGameBtn = uiDocument.rootVisualElement.Q<Button>("join-game-btn");
        _currentLobbyJoinGameBtn.RegisterCallback<ClickEvent>(evt => JoinGame());
        
        // Server Info
        _serverInfoTable = uiDocument.rootVisualElement.Q<VisualElement>("server-info-table");
        
        // List Lobbies 
        listLobbiesContainer = uiDocument.rootVisualElement.Q<VisualElement>("lobbies-container");
        listLobbiesTable = uiDocument.rootVisualElement.Q<VisualElement>("lobbies-table-body");
        
        // Lobby Modal 
        lobbyModalContainer = uiDocument.rootVisualElement.Q<VisualElement>("lobby-modal-container");
        lobbyNameInput = uiDocument.rootVisualElement.Q<TextField>("lobby-name-input");
        lobbyStatusLabel = uiDocument.rootVisualElement.Q<Label>("lobby-status-label");
        lobbyLoader = uiDocument.rootVisualElement.Q<VisualElement>("lobby-loader");
        createLobbyBtn = uiDocument.rootVisualElement.Q<Button>("create-lobby-btn");
        createLobbyBtn.RegisterCallback<ClickEvent>(evt => CreateLobby());
        cancelLobbyBtn = uiDocument.rootVisualElement.Q<Button>("cancel-lobby-btn");
        cancelLobbyBtn.RegisterCallback<ClickEvent>(evt => CancelLobby());
        
        // Game Exit Modal
        _gameExitModal = uiDocument.rootVisualElement.Q<VisualElement>("exit-modal-container");
        _confirmGameExitBtn = uiDocument.rootVisualElement.Q<Button>("confirm-exit-btn");
        _confirmGameExitBtn.RegisterCallback<ClickEvent>(evt => DisconnectClient());
        _cancelGameExitBtn = uiDocument.rootVisualElement.Q<Button>("cancel-exit-btn");
        _cancelGameExitBtn.RegisterCallback<ClickEvent>(evt => ToggleExitModal());
        
        // General 
        _backToMultiplayerMenuBtn = uiDocument.rootVisualElement.Q<Button>("back-btn");
        _backToMultiplayerMenuBtn.RegisterCallback<ClickEvent>(evt => BackToMultiplayerMenuBtn());
        _backToMultiplayerMenuBtnContainer = uiDocument.rootVisualElement.Q<VisualElement>("back-btn-container");
        _currentView = multiplayerMenuContainer;

        // Multiplayer Menu Buttons
        foreach (var button in menuBtns){

            switch (button.text){

                case "Create new lobby":
                    button.RegisterCallback<ClickEvent>(OpenCreateLobbyModal);
                    break;
                
                case "Current lobby":
                    button.RegisterCallback<ClickEvent>(evt => ListLobbyPlayers(false));
                    break;

                case "List lobbies":
                    button.RegisterCallback<ClickEvent>(evt => ListLobbies(false));
                    break;

                case "Leaderboards":
                    button.RegisterCallback<ClickEvent>(PlaceholderFunc);
                    break;

                case "Main Menu":
                    button.RegisterCallback<ClickEvent>(evt => globalState.LoadScene(button.text));
                    break;
            }
        }
    }

    void Start(){
        playerIdLabel.text = "Player ID: " + gameLobbyManager.GetPlayerID();
    }

    private void Update() {
        if (lobbyLoader != null) {
            if (timer >= 1) {
                RotateLoader();
                timer = 0;
            }
            else {
                timer += Time.deltaTime;
            }
        }
    }

    // Create a lobby and request a game server
   async void CreateLobby(){
        playerIdLabel.text = "Player ID: " + gameLobbyManager.GetPlayerID();
        
        createLobbyBtn.SetEnabled(false);
        var lobbyName = lobbyNameInput.text;
        
        ShowVisualElement(lobbyStatusLabel);
        ShowVisualElement(lobbyLoader);
        
        await gameLobbyManager.CreateLobby(lobbyName);
        
        // Update menu buttons after joining a lobby
        HideVisualElement(_createLobbyMenuBtnContainer);
        ShowVisualElement(_currentLobbyMenuBtnContainer);
          
        HideVisualElement(lobbyLoader);
        HideVisualElement(lobbyStatusLabel);
        
        HideVisualElement(lobbyModalContainer);
        await ListLobbyPlayers(false);
   }

   async Task JoinLobby(Lobby lobby) {
       await gameLobbyManager.JoinLobby(lobby);
       HideVisualElement(_createLobbyMenuBtnContainer);
       ShowVisualElement(_currentLobbyMenuBtnContainer);
       HideVisualElement(listLobbiesContainer);
       await ListLobbyPlayers(false);
   }

   async Task LeaveLobby() {
       await gameLobbyManager.LeaveLobby();
       ShowVisualElement(_createLobbyMenuBtnContainer);
       HideVisualElement(_currentLobbyMenuBtnContainer);
       ShowMultiplayerMenu();
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
               UpdateServerStatusUI();
               await gameLobbyManager.UpdateLobbyWithServerInfo(_serverStatus, clientConnectionInfo.IP, clientConnectionInfo.Port);
               NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(clientConnectionInfo.IP, (ushort)clientConnectionInfo.Port);
               NetworkManager.Singleton.StartClient();
               NetworkManager.Singleton.ConnectionApprovalCallback += OnConnectionApproval;
               NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("PlayerStatusMessage",
                   OnPlayerStatusMessage);
               await gameLobbyManager.UpdatePlayerDataWithClientId(NetworkManager.Singleton.LocalClientId);
               return true;
           }
       }
       catch (OperationCanceledException) {
           
       }
       return false;
   }

   private async Task<bool> StartClientAsLobbyPlayer() {
       
       var ip =  gameLobbyManager?.GetLobbyData("ServerIP");
       var port = gameLobbyManager?.GetLobbyData("Port");

       if (ip == null || port == null) return false;
       
       try {
           NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ip, (ushort)int.Parse(port));
           NetworkManager.Singleton.StartClient();
           NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("PlayerStatusMessage",
               OnPlayerStatusMessage);
           await gameLobbyManager.UpdatePlayerDataWithClientId(NetworkManager.Singleton.LocalClientId);
           
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
       UpdateServerStatusUI();
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
                   UpdateServerStatusUI();
                   if (_serverStatus == "ONLINE") {
                       break;
                   }
               }
               retryCount++;
               await Task.Delay(5000, cancellationToken);
           }
       }
       else {
           UpdateServerStatusUI();
       }
       
       // Now the machine is online, poll for the IP and Port of allocated game server
       var response = await webServicesAPI.PollForAllocation(60 * 5, cancellationToken);
       return response != null
           ? new ClientConnectionInfo { IP = response.ipv4, Port = response.gamePort }
           : new ClientConnectionInfo { IP = "", Port = 0 };

   }
   void CancelLobby() {
       HideVisualElement(lobbyLoader);
       HideVisualElement(lobbyModalContainer);
       ClearFormInput();
       if (cancellationTokenSource != null) {
           cancellationTokenSource.Cancel(); 
       }
   }
   
    void OpenCreateLobbyModal(ClickEvent evt){
        playerIdLabel.text = "Player ID: " + gameLobbyManager.GetPlayerID();
        ShowVisualElement(lobbyModalContainer);
    }

    async Task ListLobbyPlayers(bool updateEvent) {

        // Show/Hide buttons depending on if player is lobby host
        
        if (gameLobbyManager.IsPlayerHost()) {
            ShowVisualElement(_currentLobbyStartGameBtn);
            HideVisualElement(_currentLobbyJoinGameBtn);
        }
        else {
            ShowVisualElement(_currentLobbyJoinGameBtn);
            HideVisualElement(_currentLobbyStartGameBtn);
        }
        
        _currentView = _currentLobbyContainer;
        HideMultiplayerMenu();
        ShowVisualElement(_backToMultiplayerMenuBtnContainer);

        if (!gameLobbyManager.IsPlayerHost()) {
            _serverStatusLabel.text = gameLobbyManager?.GetLobbyData("MachineStatus");
            _serverPortLabel.text =  gameLobbyManager?.GetLobbyData("Port");
            _serverIPLabel.text =  gameLobbyManager?.GetLobbyData("ServerIP");
        }
        
        var lobbyPlayers = new List<Player>();
        
        _currentLobbyTable.Clear();

        if (!updateEvent) {
            lobbyPlayers = await gameLobbyManager.GetLobbyPlayers();
        }
        else {
            lobbyPlayers = gameLobbyManager.RefreshLobbyPlayers();
        }

        var playerCount = 0;
        var lobbyRowHeight = 100;

        foreach (var lobbyPlayer in lobbyPlayers) {

            playerCount++;
            VisualElement row = new VisualElement();
            row.AddToClassList("row-container");

            VisualElement playerId = new VisualElement();
            Label playerIdLabel = new Label();
            playerIdLabel.text = lobbyPlayer.Id;
            playerId.Add(playerIdLabel);
            playerId.AddToClassList("col-player-id");

            VisualElement playerName = new VisualElement();
            Label playerNameLabel = new Label();
            playerNameLabel.text = lobbyPlayer.Data["Name"].Value;
            playerName.Add(playerNameLabel);
            playerName.AddToClassList("col-player-name");
            
            VisualElement lastUpdated = new VisualElement();
            Label lastUpdatedLabel = new Label();
            lastUpdatedLabel.text = lobbyPlayer.Data["ClientId"].Value;
            lastUpdated.Add(lastUpdatedLabel);
            lastUpdated.AddToClassList("col-last-updated");
            
            VisualElement connectionStatus = new VisualElement();
            Label connectionStatusLabel = new Label();
            connectionStatusLabel.text = lobbyPlayer.Data["IsConnected"].Value;
            connectionStatus.Add(connectionStatusLabel);
            connectionStatus.AddToClassList("col-connection-status");
            
            VisualElement playerIsHost = new VisualElement();
            Label playerIsHostLabel = new Label();
            playerIsHostLabel.text = gameLobbyManager.IsPlayerHost(lobbyPlayer.Id) ? "Yes" : "No";
            playerIsHost.Add(playerIsHostLabel);
            playerIsHost.AddToClassList("col-is-host");
            
            row.Add(playerId);
            row.Add(playerName);
            row.Add(lastUpdated);
            row.Add(connectionStatus);
            row.Add(playerIsHost);
            
            _currentLobbyTable.Add(row);
        }
        _currentLobbyTable.style.maxHeight = playerCount * lobbyRowHeight;
        
        _currentLobbyleaveBtn.RegisterCallback<ClickEvent>(async evt => await LeaveLobby());
    }

    async void ListLobbies(bool updateEvent){

        playerIdLabel.text = "Player ID: " + gameLobbyManager.GetPlayerID();
        _currentView = listLobbiesContainer;
        HideMultiplayerMenu();
        ShowVisualElement(_backToMultiplayerMenuBtnContainer);
        listLobbiesTable.Clear();

        var lobbies = await gameLobbyManager.GetLobbiesList();
        var lobbyCount = 0;
        var lobbyRowHeight = 100;

        foreach(Lobby lobby in lobbies){

            lobbyCount++;
            VisualElement row = new VisualElement();
            row.AddToClassList("row-container");
            
            VisualElement lobbyID = new VisualElement();
            Label lobbyIDLabel = new Label();
            lobbyIDLabel.text = lobby.Id;
            lobbyID.Add(lobbyIDLabel);
            lobbyID.AddToClassList("col-lobby-id");

            VisualElement lobbyName = new VisualElement();
            Label lobbyNameLabel = new Label();
            lobbyNameLabel.text = lobby.Name;
            lobbyName.Add(lobbyNameLabel);
            lobbyName.AddToClassList("col-lobby-name");

            VisualElement players = new VisualElement();
            Label playersLabel = new Label();
            playersLabel.text = lobby.Players.Count.ToString();
            players.Add(playersLabel);
            players.AddToClassList("col-players");

            VisualElement host = new VisualElement();
            Label hostLabel = new Label();
            hostLabel.text = lobby.HostId;
            host.Add(hostLabel);
            host.AddToClassList("col-host");

            VisualElement joinLobby = new VisualElement();
            Button joinLobbyBtn = new Button();
            Label joinLobbyBtnLabel = new Label();
            joinLobbyBtnLabel.text = "Join Lobby";
            joinLobbyBtn.AddToClassList("col-join-lobby-btn");
            joinLobbyBtn.Add(joinLobbyBtnLabel);
            joinLobby.Add(joinLobbyBtn);
            joinLobby.AddToClassList("col-join-lobby");
            joinLobby.RegisterCallback<ClickEvent>(async evt => await JoinLobby(lobby));
            joinLobby.SetEnabled(!await gameLobbyManager.IsPlayerInLobby(lobby));

            row.Add(lobbyID);
            row.Add(lobbyName);
            row.Add(players);
            row.Add(host);
            row.Add(joinLobby);

            listLobbiesTable.Add(row);
        }    
        listLobbiesTable.style.maxHeight = lobbyCount * lobbyRowHeight;
    }
    void BackToMultiplayerMenuBtn(){
        
        HideVisualElement(_backToMultiplayerMenuBtnContainer);

        if (_currentView == listLobbiesContainer) {
            listLobbiesTable.style.maxHeight = 0;
            HideVisualElement(_backToMultiplayerMenuBtnContainer);
            ShowMultiplayerMenu();
        }
        else if (_currentView == _currentLobbyContainer) {
            _currentLobbyTable.style.maxHeight = 0;
            HideVisualElement(_backToMultiplayerMenuBtnContainer);
            ShowMultiplayerMenu();
        }
    }
    
    void HideVisualElement(VisualElement element){
        if (element != null) {
            element.style.display = DisplayStyle.None;
        }
    }

    void ShowVisualElement(VisualElement element){
        if (element != null) {
            element.style.display = DisplayStyle.Flex;
        }
    }

    void HideMultiplayerMenu(){
        multiplayerMenuContainer.style.display = DisplayStyle.None;
        _currentView.style.display = DisplayStyle.Flex;
    }

    void ShowMultiplayerMenu(){
        multiplayerMenuContainer.style.display = DisplayStyle.Flex;
        _currentView.style.display = DisplayStyle.None;
        _currentView = multiplayerMenuContainer;
    }

    void RotateLoader() {
        rotation += 360;
        lobbyLoader.style.rotate =
            new StyleRotate(new UnityEngine.UIElements.Rotate(new Angle(rotation, AngleUnit.Degree)));
    }

    void PlaceholderFunc(ClickEvent evt){
        var targetButton = evt.target as Button;
        Debug.Log(targetButton.text);
    }

    void ClearFormInput() {
        lobbyNameInput.SetValueWithoutNotify("");
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
        
        gameLobbyManager.ApplyLobbyChanges(changes);

        if (_currentView == _currentLobbyContainer) {
            await ListLobbyPlayers(true);
        }
        else if (_currentView == listLobbiesContainer) {
            ListLobbies(true);
        }
    }

    // Start game server as lobby host
    private async Task StartGame() {
        _currentLobbyStartGameBtn.SetEnabled(false);
        cancellationTokenSource = new CancellationTokenSource();
        var clientConnected = await StartClientAsLobbyHost(cancellationTokenSource.Token);
      
        if (clientConnected) {
            Debug.Log("client connected!");
            HideVisualElement(mainContainer);
        }
    }

    private async void JoinGame() {
        var clientConnected = await StartClientAsLobbyPlayer();
        if (clientConnected) {
            Debug.Log("Lobby player joined!");
            HideVisualElement(mainContainer);
        }
    }

    private void UpdateServerStatusUI() {
        if (_currentView == _currentLobbyContainer) {
            _serverStatusLabel.text = _serverStatus;
            _serverPortLabel.text = _serverPort;
            _serverIPLabel.text = _serverIP;
            Debug.Log("UpdateServerStatusUI");
        }
    }

    private void ToggleExitModal() {
        Debug.Log("Escape key pressed!");
        if (_gameExitModal.style.display == DisplayStyle.Flex) {
            HideVisualElement(_gameExitModal);
        }
        else {
            ShowVisualElement(_gameExitModal);
        }
    }

    private void OnEscape(InputAction.CallbackContext context) {
        ToggleExitModal();
    }

    private async void DisconnectClient() {
        NetworkManager.Singleton.Shutdown();
        ToggleExitModal();
        await ListLobbyPlayers(false);
        ShowVisualElement(mainContainer);
    }
    
    void OnEnable(){
        controls = new Controls();
        controls.UI.Enable();
        controls.UI.ESC.performed += OnEscape;
    }

    void OnDisable(){
        controls.UI.ESC.performed -= OnEscape;
        controls.UI.Disable();
    }

    private async void OnPlayerStatusMessage(ulong clientId, FastBufferReader reader) {
        PlayerStatusMessage msg = new PlayerStatusMessage();
        reader.ReadNetworkSerializable<PlayerStatusMessage>(out msg);
        Debug.Log("Player Status Message: " + msg.ClientId + " Connected: " + msg.IsConnected);
        await gameLobbyManager.UpdatePlayerDataWithConnectionStatus(msg.IsConnected, msg.ClientId.ToString());
    }

    private void OnConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response) {
        response.Approved = true;
        response.CreatePlayerObject = true;

        // Include the player name in the connection approval payload
        var playerData = new PlayerData {
            ClientId = NetworkManager.Singleton.LocalClientId,
            IsConnected = true
        };

        using (var writer = new FastBufferWriter(128, Allocator.Temp)) {
            writer.WriteNetworkSerializable(playerData);
            request.Payload = writer.ToArray();
        }
    }
}
