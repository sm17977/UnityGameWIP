using System.Collections.Generic;
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
    
    List<Button> menuBtns;

    // Input System
    private Controls controls;

    // Global State
    private Global_State globalState;

    public TestLobby testLobby;


    void Awake(){

        globalState = GameObject.Find("Global State").GetComponent<Global_State>();
        testLobby = GameObject.Find("TestLobby").GetComponent<TestLobby>();

        menuBtnsContainer = uiDocument.rootVisualElement.Query<VisualElement>("menu-container");
        menuBtns = uiDocument.rootVisualElement.Query<Button>("btn").ToList();

        listLobbiesContainer = uiDocument.rootVisualElement.Query<VisualElement>("lobby-container");
        listLobbiesBackBtn = uiDocument.rootVisualElement.Query<Button>("lobby-back-btn");
        listLobbiesTable = uiDocument.rootVisualElement.Query<VisualElement>("lobby-table-body");
        listLobbiesBackBtn.RegisterCallback<ClickEvent>(evt => ShowMenu(listLobbiesContainer));

        foreach (var button in menuBtns){

            switch (button.text){

                case "Create new lobby":
                    button.RegisterCallback<ClickEvent>(PlaceholderFunc);
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


    async void ListLobbies(ClickEvent evt){

        HideMenu(listLobbiesContainer);
        listLobbiesTable.Clear();
        var lobbies = await testLobby.GetLobbiesList();

        foreach(Lobby lobby in lobbies){

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
            Label playersLable = new Label();
            playersLable.text = lobby.Players.Count.ToString();
            players.Add(playersLable);
            players.AddToClassList("col-body");

            Button joinLobby = new Button();
            joinLobby.RegisterCallback<ClickEvent>(evt => testLobby.Join(lobby));
            Label joinLobbyLabel = new Label();
            joinLobbyLabel.text = "Join Lobby";
            joinLobby.Add(joinLobbyLabel);
            joinLobby.AddToClassList("col-body");

            row.Add(lobbyID);
            row.Add(lobbyName);
            row.Add(players);
            row.Add(joinLobby);

            listLobbiesTable.Add(row);
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
