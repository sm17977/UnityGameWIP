using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Multiplayer;
using QFSW.QC;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UIElements;
using WebSocketSharp;

public class Multiplayer_UI_Controller : MonoBehaviour
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
    private Button _currentLobbyPlayBtn;
    private Button _currentLobbyleaveBtn;
    
    // Lobbies List
    VisualElement listLobbiesContainer;
    VisualElement listLobbiesTable;
    
    // Create Lobby Modal
    Button createLobbyBtn;
    Button cancelLobbyBtn;
    VisualElement lobbyModalContainer;
    TextField lobbyNameInput;
    private Label lobbyStatusLabel;
    
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
        _currentLobbyPlayBtn = uiDocument.rootVisualElement.Q<Button>("current-lobby-play-btn");
        _currentLobbyleaveBtn = uiDocument.rootVisualElement.Q<Button>("leave-lobby-btn");
        
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
                    button.RegisterCallback<ClickEvent>(evt => ListLobbyPlayers());
                    break;

                case "List lobbies":
                    button.RegisterCallback<ClickEvent>(ListLobbies);
                    break;

                case "Leaderboards":
                    button.RegisterCallback<ClickEvent>(PlaceholderFunc);
                    break;

                case "Main Menu":
                    button.RegisterCallback<ClickEvent>(evt => globalState.LoadScene(button.text));
                    break;
            }
        }

        Debug.Log("here");
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
        
        HideVisualElement(lobbyModalContainer);
        
        ListLobbyPlayers();
        
        
        
        // cancellationTokenSource = new CancellationTokenSource();
        // var clientConnected = await StartClient(cancellationTokenSource.Token);
        //
        // HideVisualElement(lobbyLoader);
        // HideVisualElement(lobbyStatusLabel);
        // if (clientConnected) {
        //     HideVisualElement(lobbyModalContainer);
        //     ShowVisualElement(_currentLobbyContainer);
        // }
        // createLobbyBtn.SetEnabled(true);
   }

   async Task JoinLobby(Lobby lobby) {
       await gameLobbyManager.JoinLobby(lobby);
       HideVisualElement(_createLobbyMenuBtnContainer);
       ShowVisualElement(_currentLobbyMenuBtnContainer);
       HideVisualElement(listLobbiesContainer);
       ListLobbyPlayers();
   }

   async Task LeaveLobby() {
       await gameLobbyManager.LeaveLobby();
   }
   

   // TODO - Ensure only lobby hosts can request a server allocation
   private async Task<bool> StartClient(CancellationToken cancellationToken) {

       try {
           WebServicesAPI webServicesAPI = new WebServicesAPI();
           await webServicesAPI.RequestAPIToken();
           var clientConnectionInfo = await GetClientConnectionInfo(cancellationToken, webServicesAPI);
           if (!clientConnectionInfo.IP.IsNullOrEmpty()) {
               await gameLobbyManager.UpdateLobbyWithServerInfo("test", clientConnectionInfo.IP);
               NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(clientConnectionInfo.IP, (ushort)clientConnectionInfo.Port);
               NetworkManager.Singleton.StartClient();
               return true;
           }
       }
       catch (OperationCanceledException) {
           
       }
       return false;
   }
   
   private async Task<ClientConnectionInfo> GetClientConnectionInfo(CancellationToken cancellationToken, WebServicesAPI webServicesAPI) {
            
       // Queue allocation request for a game server
       await webServicesAPI.QueueAllocationRequest();
       
       // Check for active machine
       var machineStatus = await webServicesAPI.GetMachineStatus();
       
       // If no machine is found yet, keep polling until one is
       if (machineStatus.IsNullOrEmpty()) {
           var machines = await webServicesAPI.PollForMachine(60 * 5, cancellationToken, false);
           var initStatus = machines[0].status;
           lobbyStatusLabel.text = initStatus;
           
           while (true) {
               var status = await webServicesAPI.GetMachineStatus();
               if (status != "" && status != initStatus) {
                   lobbyStatusLabel.text = status;
                   break;
               }
           }
       }
       else {
           lobbyStatusLabel.text = machineStatus;
       }
            
       // Get the IP and Port of allocated game server
       var response = await webServicesAPI.PollForAllocation(60 * 5, cancellationToken);

       if (response != null) {
           return new ClientConnectionInfo { IP = response.ipv4, Port = response.gamePort };
       }

       return new ClientConnectionInfo { IP = "", Port = 0 };
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

    async void ListLobbyPlayers() {

        _currentView = _currentLobbyContainer;
        HideMultiplayerMenu();
        ShowVisualElement(_backToMultiplayerMenuBtnContainer);
        
        _currentLobbyTable.Clear();

        var lobbyPlayers = await gameLobbyManager.GetLobbyPlayers();
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
            playerNameLabel.text = lobbyPlayer.Data["name"].Value;
            playerName.Add(playerNameLabel);
            playerName.AddToClassList("col-player-name");
            
            VisualElement lastUpdated = new VisualElement();
            Label lastUpdatedLabel = new Label();
            lastUpdatedLabel.text = lobbyPlayer.LastUpdated.ToString();
            lastUpdated.Add(lastUpdatedLabel);
            lastUpdated.AddToClassList("col-last-updated");
            
            VisualElement playerIsHost = new VisualElement();
            Label playerIsHostLabel = new Label();
            playerIsHostLabel.text = gameLobbyManager.IsPlayerHost(lobbyPlayer.Id) ? "Yes" : "No";
            playerIsHost.Add(playerIsHostLabel);
            playerIsHost.AddToClassList("col-is-host");
            
            row.Add(playerId);
            row.Add(playerName);
            row.Add(lastUpdated);
            row.Add(playerIsHost);
            
            _currentLobbyTable.Add(row);
        }
        _currentLobbyTable.style.maxHeight = playerCount * lobbyRowHeight;
        
        _currentLobbyleaveBtn.RegisterCallback<ClickEvent>(async evt => await LeaveLobby());
    }

    async void ListLobbies(ClickEvent evt){

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

    void OnEnable(){
        controls = new Controls();
        controls.UI.Enable();
    }

    void OnDisable(){
        controls.UI.Disable();
    }
}
