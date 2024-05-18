using System.Collections.Generic;
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

    // Input System
    private Controls controls;

    // Global State
    private Global_State globalState;
    public GameObject gameLobbyManagerObj;
    private GameLobbyManager gameLobbyManager;

    void Awake(){

        globalState = GameObject.Find("Global State").GetComponent<Global_State>();
        gameLobbyManager = gameLobbyManagerObj.GetComponent<GameLobbyManager>();

        playerIdLabel = uiDocument.rootVisualElement.Q<Label>("player-id");
        menuBtnsContainer = uiDocument.rootVisualElement.Q<VisualElement>("menu-container");
        menuBtns = uiDocument.rootVisualElement.Query<Button>("btn").ToList();

        listLobbiesContainer = uiDocument.rootVisualElement.Q<VisualElement>("lobby-container");
        listLobbiesBackBtn = uiDocument.rootVisualElement.Q<Button>("lobby-back-btn");
        listLobbiesTable = uiDocument.rootVisualElement.Q<VisualElement>("lobby-table-body");
        listLobbiesBackBtn.RegisterCallback<ClickEvent>(evt => ListLobbyBackBtn());
        listLobbiesBackBtnContainer = uiDocument.rootVisualElement.Q<VisualElement>("back-btn-container");
        
        lobbyModalContainer = uiDocument.rootVisualElement.Q<VisualElement>("lobby-modal-container");
        lobbyNameInput = uiDocument.rootVisualElement.Q<TextField>("lobby-name-input");
        createLobbyBtn = uiDocument.rootVisualElement.Q<Button>("create-lobby-btn");
        createLobbyBtn.RegisterCallback<ClickEvent>(evt => CreateLobby());
        cancelLobbyBtn = uiDocument.rootVisualElement.Q<Button>("cancel-lobby-btn");
        cancelLobbyBtn.RegisterCallback<ClickEvent>(evt => HideVisualElement(lobbyModalContainer));

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
    }

    void Start(){
        playerIdLabel.text = "Player ID: " + gameLobbyManager.GetPlayerID();
    }

   async void CreateLobby(){
        playerIdLabel.text = "Player ID: " + gameLobbyManager.GetPlayerID();
        string lobbyName = lobbyNameInput.text;
        await gameLobbyManager.CreateLobby(lobbyName);
        HideVisualElement(lobbyModalContainer);
        ListLobbies(new ClickEvent());
        await StartClient();
   }

   private async Task StartClient() {
       WebServicesAPI webServicesAPI = new WebServicesAPI();
       await webServicesAPI.GetServerList();
       await webServicesAPI.RequestAPIToken();
       await webServicesAPI.QueueAllocationRequest();
       GetAllocationResponse response = await webServicesAPI.GetAllocationRequest();

       string ipv4Address = "35.233.17.110";//response.ipv4;
       ushort port = 9000;//(ushort)response.gamePort;
       NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ipv4Address, port);
       NetworkManager.Singleton.StartClient();
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
        element.style.display = DisplayStyle.None;
    }

    void ShowVisualElement(VisualElement element){
        element.style.display = DisplayStyle.Flex;
    }

    void HideMenu(VisualElement selectedItem){
        menuBtnsContainer.style.display = DisplayStyle.None;
        selectedItem.style.display = DisplayStyle.Flex;
    }

    void ShowMenu(VisualElement selectedItem){
        menuBtnsContainer.style.display = DisplayStyle.Flex;
        selectedItem.style.display = DisplayStyle.None;
    }

    void PlaceholderFunc(ClickEvent evt){
        var targetButton = evt.target as Button;
        Debug.Log(targetButton.text);
    }

    void OnEnable(){
        controls = new Controls();
        controls.UI.Enable();
    }

    void OnDisable(){
        controls.UI.Disable();
    }
}
