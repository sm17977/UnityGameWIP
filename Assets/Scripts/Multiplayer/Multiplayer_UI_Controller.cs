using System.Collections.Generic;
using QFSW.QC;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UIElements;

public class Multiplayer_UI_Controller : MonoBehaviour
{
 
    // UI Document
    [SerializeField] private UIDocument uiDocument;

    // Visual Elements

    VisualElement menuBtnsContainer;
    VisualElement listLobbiesContainer;
    VisualElement listLobbiesTable;
    Button listLobbiesBackBtn;
    VisualElement lobbyModalContainer;
    
    List<Button> menuBtns;

    // Input System
    private Controls controls;

    // Global State
    private Global_State globalState;
    public GameObject gameLobbyManagerObj;
    private GameLobbyManager gameLobbyManager;

    void Awake(){

        globalState = GameObject.Find("Global State").GetComponent<Global_State>();
        gameLobbyManager = gameLobbyManagerObj.GetComponent<GameLobbyManager>();

        menuBtnsContainer = uiDocument.rootVisualElement.Query<VisualElement>("menu-container");
        menuBtns = uiDocument.rootVisualElement.Query<Button>("btn").ToList();

        listLobbiesContainer = uiDocument.rootVisualElement.Query<VisualElement>("lobby-container");
        listLobbiesBackBtn = uiDocument.rootVisualElement.Query<Button>("lobby-back-btn");
        listLobbiesTable = uiDocument.rootVisualElement.Query<VisualElement>("lobby-table-body");
        listLobbiesBackBtn.RegisterCallback<ClickEvent>(evt => ListLobbyBackBtn());

        lobbyModalContainer = uiDocument.rootVisualElement.Query<VisualElement>("lobby-modal-container");

        foreach (var button in menuBtns){

            switch (button.text){

                case "Create new lobby":
                    button.RegisterCallback<ClickEvent>(CreateLobby);
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


    async void CreateLobby(ClickEvent evt){
        lobbyModalContainer.style.display = DisplayStyle.Flex;
        // await gameLobbyManager.CreateLobby();
        // ListLobbies(evt);
    }

    async void ListLobbies(ClickEvent evt){

        HideMenu(listLobbiesContainer);
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
            lobbyID.AddToClassList("col-body");

            VisualElement lobbyName = new VisualElement();
            Label lobbyNameLabel = new Label();
            lobbyNameLabel.text = lobby.Name;
            lobbyName.Add(lobbyNameLabel);
            lobbyName.AddToClassList("col-body");

            VisualElement players = new VisualElement();
            Label playersLabel = new Label();
            playersLabel.text = lobby.Players.Count.ToString();
            players.Add(playersLabel);
            players.AddToClassList("col-body");

            VisualElement joinLobby = new VisualElement();
            Button joinLobbyBtn = new Button();
            Label joinLobbyBtnLabel = new Label();
            joinLobbyBtnLabel.text = "Join Lobby";
            joinLobbyBtn.Add(joinLobbyBtnLabel);
            joinLobby.Add(joinLobbyBtn);
            joinLobby.AddToClassList("col-body");
            joinLobby.RegisterCallback<ClickEvent>(async evt => await gameLobbyManager.JoinLobby(lobby));
            joinLobby.SetEnabled(!await gameLobbyManager.IsPlayerInLobby(lobby));

            row.Add(lobbyID);
            row.Add(lobbyName);
            row.Add(players);
            row.Add(joinLobby);

            listLobbiesTable.Add(row);
         }    

         listLobbiesTable.style.maxHeight = lobbyCount * lobbyRowHeight;
    }

    void ListLobbyBackBtn(){
        listLobbiesTable.style.maxHeight = 0;
        ShowMenu(listLobbiesContainer);
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
