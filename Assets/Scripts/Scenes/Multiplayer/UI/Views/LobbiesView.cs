using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    
    public delegate Task OnJoinLobby(Lobby lobby);
    public delegate Task<List<Lobby>> OnGetLobbyTableData(bool sendNewRequest);
    public delegate Task<bool> OnIsPlayerInLobby(Lobby lobby);
    public class LobbiesView : View {

        public event OnJoinLobby JoinLobby;
        public event OnGetLobbyTableData GetLobbyTableData;
        public event OnIsPlayerInLobby IsPlayerInLobby;
        
        private VisualElement _table;
        private VisualElement _selectedRow = null;
        private Button _backBtn;
        private Button _joinBtn;
        private VisualElement _joinBtnContainer;
        private List<Lobby> _lobbies;
        
        public LobbiesView(VisualElement parentContainer, VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();
            ParentContainer = parentContainer;
            BindUIElements();
        }
        private void BindUIElements() {
            _table = Template.Q<VisualElement>("lobbies-table-body");
            
            _backBtn = Template.Q<Button>("back-btn");
            _backBtn.RegisterCallback<ClickEvent>(evt => {
                Reset();
                OnReturnToMultiplayerMenu();
            });

            _joinBtn = Template.Q<Button>("join-lobby-btn");
            _joinBtnContainer = Template.Q<VisualElement>("join-lobby-btn-container");
            
            _joinBtn.RegisterCallback<ClickEvent>(evt => OnJoinLobbyButtonClick(evt));
            
            _joinBtn.SetEnabled(false);
            _joinBtnContainer.AddToClassList("join-btn-shadow-disabled");
        }
        public override async void Show() {
            base.Show();
            _table.Clear();
            await GenerateLobbiesTable(true);
        }
        
        public override void Hide() {
            base.Hide();
            //_table.style.height = 0;
        }
        
        private async Task GenerateLobbiesTable(bool sendNewRequest) {
            
            _lobbies = await GetLobbyTableData?.Invoke(sendNewRequest);
            if (_lobbies == null) return;
            
            var lobbyCount = 0;
            // var lobbyRowHeight = 24;

            foreach(var lobby in _lobbies){

                lobbyCount++;
                VisualElement row = new VisualElement();
                row.AddToClassList("lobbies-table-row");
                
                // This column is hidden, it's just used for joining a lobby
                VisualElement lobbyId = new VisualElement();
                Label lobbyIdLabel = new Label();
                lobbyIdLabel.name = "lobby-id";
                lobbyIdLabel.text = lobby.Id;
                lobbyId.Add(lobbyIdLabel);
                lobbyId.AddToClassList("lobbies-lobby-id");
                
                VisualElement lobbyName = new VisualElement();
                Label lobbyNameLabel = new Label();
                lobbyNameLabel.text = lobby.Name;
                lobbyName.Add(lobbyNameLabel);
                lobbyName.AddToClassList("lobbies-col-width");
                
                VisualElement host = new VisualElement();
                Label hostLabel = new Label();
                hostLabel.text = lobby.Players.Find((player => player.Id == lobby.HostId)).Data["Name"].Value;
                host.Add(hostLabel);
                host.AddToClassList("lobbies-col-width");
                
                VisualElement gameMode = new VisualElement();
                Label gameModeLabel = new Label();
                gameModeLabel.text = lobby.Data["GameMode"].Value;
                gameMode.Add(gameModeLabel);
                gameMode.AddToClassList("lobbies-col-width");

                VisualElement players = new VisualElement();
                Label playersLabel = new Label();
                playersLabel.text = lobby.Players.Count.ToString();
                players.Add(playersLabel);
                players.AddToClassList("lobbies-col-width");
                
                row.Add(lobbyId);
                row.Add(lobbyName);
                row.Add(host);
                row.Add(gameMode);
                row.Add(players);
                
                row.RegisterCallback<ClickEvent>((evt) => OnLobbyRowClick(evt));
                
                _table.Add(row);
            }    
            // _table.style.height = lobbyCount * lobbyRowHeight;
        }

        private async void OnJoinLobbyButtonClick(ClickEvent evt) {
            if (_selectedRow != null) {
                var lobbyId = _selectedRow.Q<Label>("lobby-id").text;
                var lobbyToJoin = _lobbies.Find((lobby) => lobby.Id == lobbyId);
                await JoinLobby?.Invoke(lobbyToJoin);
                Reset();
            }
        }

        private void OnLobbyRowClick(ClickEvent evt) {
            // Select row
            var row = evt.currentTarget as VisualElement;
            if (row == null) return;
            
            _joinBtn.SetEnabled(true);
            _joinBtnContainer.RemoveFromClassList("join-btn-shadow-disabled");
            
            if (row != _selectedRow) {
                if (_selectedRow != null) {
                    _selectedRow.RemoveFromClassList("lobbies-table-row-selected");
                }
                _selectedRow = row;
                row.ToggleInClassList("lobbies-table-row-selected");
            }
        }

        private void Reset() {
            if (_selectedRow != null) {
                _selectedRow.RemoveFromClassList("lobbies-table-row-selected");
                _selectedRow = null;
            }
            _joinBtn.SetEnabled(false);
            _joinBtnContainer.AddToClassList("join-btn-shadow-disabled");
        }
        
        public override async void Update() {
            _table.Clear();
            await GenerateLobbiesTable(true);
        }

        public override async void RePaint() {
            _table.Clear();
            await GenerateLobbiesTable(false);
        }
    }
}