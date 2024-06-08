using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    public class LobbiesView : View {

        private MultiplayerUIController _uiController;
        private VisualElement _table;
        
        private Button _backBtn;    
        
        public LobbiesView(VisualElement parentContainer, MultiplayerUIController uiController, VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();
            ParentContainer = parentContainer;
            _uiController = uiController;
            InitializeElements();
        }
        private void InitializeElements() {
            _table = Template.Q<VisualElement>("lobbies-table-body");
            
            _backBtn = Template.Q<Button>("back-btn");
            _backBtn.RegisterCallback<ClickEvent>(evt => OnClickBackBtn());
        }
        public override async void Show() {
            base.Show();
            Debug.Log("Lobbies Frame Showing Element: "  + Time.frameCount);
            _table.Clear();
            await GenerateLobbiesTable(true);
        }
        
        public override void Hide() {
            base.Hide();
            _table.style.height = 0;
        }
        
        private void OnClickBackBtn() {
            _uiController.ReturnToMultiplayerMenu();
        }
        private async Task GenerateLobbiesTable(bool sendNewRequest) {

            var lobbies = await _uiController.GetLobbyTableData(sendNewRequest);
            var lobbyCount = 0;
            var lobbyRowHeight = 24;

            foreach(var lobby in lobbies){

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
                joinLobby.RegisterCallback<ClickEvent>( evt => OnClickJoinLobbyBtn(lobby));
                joinLobby.SetEnabled(!await IsPlayerInLobby(lobby));

                row.Add(lobbyID);
                row.Add(lobbyName);
                row.Add(players);
                row.Add(host);
                row.Add(joinLobby);

                _table.Add(row);
            }    
            _table.style.height = lobbyCount * lobbyRowHeight;
        }
        
        
        private async void OnClickJoinLobbyBtn(Lobby lobby) {
            await _uiController.JoinLobby(lobby);
        }

        private async Task<bool> IsPlayerInLobby(Lobby lobby) {
            return await _uiController.IsPlayerInLobby(lobby);
        }
        
        public override void Update() {
            _table.Clear();
            Debug.Log("updating lobbies");
            GenerateLobbiesTable(true);
        }

        public override void RePaint() {
            _table.Clear();
            Debug.Log("repaitning lobbies");
            GenerateLobbiesTable(false);
        }
    }
}