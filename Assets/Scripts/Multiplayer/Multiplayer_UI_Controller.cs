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


public class Multiplayer_UI_Controller : MonoBehaviour
{
 
    // UI Document
    [SerializeField] private UIDocument uiDocument;

    // Visual Elements

    // Menu
    VisualElement mainContainer;
    VisualElement menuBtnsContainer;
    List<Button> menuBtns;
    Label playerIdLabel;

    // Lobbies List
    VisualElement listLobbiesContainer;
    VisualElement listLobbiesTable;
    VisualElement listLobbiesBackBtnContainer;
    Button listLobbiesBackBtn;

    // Create Lobby Modal
    Button createLobbyBtn;
    Button cancelLobbyBtn;
    VisualElement lobbyModalContainer;
    TextField lobbyNameInput;
    
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
    
    //
    private CancellationTokenSource cancellationTokenSource;
    

    void Awake(){

        globalState = GameObject.Find("Global State").GetComponent<Global_State>();
        gameLobbyManager = gameLobbyManagerObj.GetComponent<GameLobbyManager>();

        playerIdLabel = uiDocument.rootVisualElement.Q<Label>("player-id");
        mainContainer = uiDocument.rootVisualElement.Q<VisualElement>("main-container");
        menuBtnsContainer = uiDocument.rootVisualElement.Q<VisualElement>("menu-container");
        menuBtns = uiDocument.rootVisualElement.Query<Button>("btn").ToList();

        listLobbiesContainer = uiDocument.rootVisualElement.Q<VisualElement>("lobby-container");
        listLobbiesBackBtn = uiDocument.rootVisualElement.Q<Button>("lobby-back-btn");
        listLobbiesTable = uiDocument.rootVisualElement.Q<VisualElement>("lobby-table-body");
        listLobbiesBackBtn.RegisterCallback<ClickEvent>(evt => ListLobbyBackBtn());
        listLobbiesBackBtnContainer = uiDocument.rootVisualElement.Q<VisualElement>("back-btn-container");
        
        lobbyModalContainer = uiDocument.rootVisualElement.Q<VisualElement>("lobby-modal-container");
        lobbyNameInput = uiDocument.rootVisualElement.Q<TextField>("lobby-name-input");
        lobbyLoader = uiDocument.rootVisualElement.Q<VisualElement>("lobby-loader");
        createLobbyBtn = uiDocument.rootVisualElement.Q<Button>("create-lobby-btn");
        createLobbyBtn.RegisterCallback<ClickEvent>(evt => CreateLobby());
        cancelLobbyBtn = uiDocument.rootVisualElement.Q<Button>("cancel-lobby-btn");
        cancelLobbyBtn.RegisterCallback<ClickEvent>(evt => CancelLobby());

        foreach (var button in menuBtns){

            switch (button.text){

                case "Create new lobby":
                    button.RegisterCallback<ClickEvent>(OpenCreateLobbyModal);
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
       createLobbyBtn.SetEnabled(false);
        playerIdLabel.text = "Player ID: " + gameLobbyManager.GetPlayerID();
        string lobbyName = lobbyNameInput.text;
        ShowVisualElement(lobbyLoader);
        await gameLobbyManager.CreateLobby(lobbyName);
        cancellationTokenSource = new CancellationTokenSource();
        var clientConnected = await StartClient(cancellationTokenSource.Token);
        HideVisualElement(lobbyLoader);
        if (clientConnected) {
            HideVisualElement(mainContainer);
        }
        createLobbyBtn.SetEnabled(true);
   }

   // TODO - Ensure only lobby hosts can request a server allocation
   private async Task<bool> StartClient(CancellationToken cancellationToken) {

       try {
           WebServicesAPI webServicesAPI = new WebServicesAPI();
           await webServicesAPI.GetServerList();
           await webServicesAPI.RequestAPIToken();
           await webServicesAPI.QueueAllocationRequest();

           GetAllocationResponse response =
               await webServicesAPI.PollForAllocation(60 * 5, cancellationToken);

           if (response != null && !string.IsNullOrEmpty(response.ipv4)) {
               string ipv4Address = response.ipv4;
               ushort port = (ushort)response.gamePort;
               NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ipv4Address, port);
               NetworkManager.Singleton.StartClient();
               Debug.Log("Client started successfully.");
               return true;
           }
       }
       catch (OperationCanceledException) {
           Debug.Log("Cancelled allocation request");
       }

       Debug.LogError("Failed to retrieve server allocation details.");
       return false;
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

    async void ListLobbies(ClickEvent evt){

        playerIdLabel.text = "Player ID: " + gameLobbyManager.GetPlayerID();
        HideMenu(listLobbiesContainer);
        ShowVisualElement(listLobbiesBackBtnContainer);
        listLobbiesTable.Clear();

        var lobbies = await gameLobbyManager.GetLobbiesList();
        int lobbyCount = 0;
        int lobbyRowHeight = 100;

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
            joinLobby.RegisterCallback<ClickEvent>(async evt => await gameLobbyManager.JoinLobby(lobby));
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

    void ListLobbyBackBtn(){
        listLobbiesTable.style.maxHeight = 0;
        HideVisualElement(listLobbiesBackBtnContainer);
        ShowMenu(listLobbiesContainer);
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

    void HideMenu(VisualElement selectedItem){
        menuBtnsContainer.style.display = DisplayStyle.None;
        selectedItem.style.display = DisplayStyle.Flex;
    }

    void ShowMenu(VisualElement selectedItem){
        menuBtnsContainer.style.display = DisplayStyle.Flex;
        selectedItem.style.display = DisplayStyle.None;
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
