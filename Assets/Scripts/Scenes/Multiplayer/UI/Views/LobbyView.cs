using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    public class LobbyView : View {
        
        private MultiplayerUIController _uiController;
        
        private VisualElement _table;
        private Button _startGameBtn;
        private Button _joinGameBtn;
        private Button _leaveBtn;
        
        private Button _backBtn;    
    
        private VisualElement _serverInfoTable;
        private Label _serverStatusLabel;
        private Label _serverIPLabel;
        private Label _serverPortLabel;

        private string _serverStatus;
        private string _serverIP;
        private string _serverPort;
        private string _playerConnectionStatus;
        
        private static readonly Dictionary<string, string> ServerStatusClasses = new Dictionary<string, string>
        {
            { "SHUTDOWN", "server-status-default" },
            { "BOOTING", "server-status-booting" },
            { "AWAITING_SETUP", "server-status-awaiting-setup" },
            { "ONLINE", "server-status-online" },
            { "Inactive", "server-status-default" }
        };
        
        public LobbyView(VisualElement parentContainer, MultiplayerUIController uiController, VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();
            ParentContainer = parentContainer;
            _uiController = uiController;
            InitializeElements();
        }

        private void InitializeElements() {
            _serverStatusLabel = Template.Q<Label>("server-status-label");
            _serverIPLabel = Template.Q<Label>("server-ip-label");
            _serverPortLabel = Template.Q<Label>("server-port-label");
            
            _table = Template.Q<VisualElement>("current-lobby-table-body");
            _leaveBtn = Template.Q<Button>("leave-lobby-btn");
            _startGameBtn = Template.Q<Button>("start-game-btn");
            _joinGameBtn = Template.Q<Button>("join-game-btn");
            _backBtn = Template.Q<Button>("back-btn");
            
            _startGameBtn.RegisterCallback<ClickEvent>(evt => OnClickStartGameBtn());
            _joinGameBtn.RegisterCallback<ClickEvent>(evt => OnClickJoinGameBtn());
            _leaveBtn.RegisterCallback<ClickEvent>( evt => OnClickLeaveLobbyBtn());
            _backBtn.RegisterCallback<ClickEvent>(evt => OnClickBackBtn());
        }

        public override async void Show() {
            base.Show();
            await Task.Delay(50);
;           _table.Clear();
            DisplayButtons();
            await GenerateLobbyPlayerTable(false);
        }

        public override void Hide() {
            base.Hide();
            _table.style.height = 0;
        }

        private void OnClickBackBtn() {
            _uiController.ReturnToMultiplayerMenu();
        }
        private async void OnClickStartGameBtn() {
            _startGameBtn.SetEnabled(false);
            bool clientConnected = await _uiController.StartGame();
            
            if (clientConnected) {
            }
        }
        private async void OnClickJoinGameBtn() {
            _joinGameBtn.SetEnabled(false);
            bool clientConnected = await _uiController.JoinGame();
            
            if (clientConnected) {
                _joinGameBtn.SetEnabled(true);
            }
        }

        private async void OnClickLeaveLobbyBtn() {
            await _uiController.LeaveLobby();
        }
        
        private void DisplayButtons() {
            var isPlayerHost = _uiController.IsPlayerHost();
            if (isPlayerHost) {
                _startGameBtn.SetEnabled(_uiController.CanStartGame());
                Show(_startGameBtn);
                Hide(_joinGameBtn);
            }
            else {
                _joinGameBtn.SetEnabled(_uiController.CanJoinGame());
                Show(_joinGameBtn);
                Hide(_startGameBtn);
            }
        }
        
        private void UpdateServerInfoTable() {
            
            _serverStatusLabel.text = _uiController.Client.ServerStatus;
            _serverPortLabel.text = _uiController.Client.Port;
            _serverIPLabel.text = _uiController.Client.ServerIP;
            
            if (_uiController.Client.ServerStatus != null &&  _uiController.Client.ServerStatus != "") {
                ApplyServerStatusStyling();
            }
        }

        private async Task GenerateLobbyPlayerTable(bool sendNewRequest) {

            var lobbyPlayers = await _uiController.GetLobbyPlayerTableData(sendNewRequest);
            var playerCount = 0;
            var lobbyRowHeight = 24;

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
                lastUpdatedLabel.text = lobbyPlayer.LastUpdated.ToString();
                lastUpdated.Add(lastUpdatedLabel);
                lastUpdated.AddToClassList("col-last-updated");
                
                VisualElement connectionStatus = new VisualElement();
                VisualElement connectionStatusInner = new VisualElement();
                Label connectionStatusLabel = new Label();
                var connected = bool.Parse(lobbyPlayer.Data["IsConnected"].Value);
                connectionStatusLabel.text = connected ? "Connected" : "Not Connected";
                connectionStatusLabel.AddToClassList(connected ? "player-connection-status-connected" : "player-connection-status-not-connected");
                connectionStatusInner.Add(connectionStatusLabel);
                connectionStatusInner.AddToClassList("col-connection-status-inner");
                connectionStatus.Add(connectionStatusInner);
                connectionStatus.AddToClassList("col-connection-status");
                
                VisualElement playerIsHost = new VisualElement();
                Label playerIsHostLabel = new Label();
                playerIsHostLabel.text = _uiController.IsPlayerHost(lobbyPlayer.Id) ? "Yes" : "No";
                playerIsHost.Add(playerIsHostLabel);
                playerIsHost.AddToClassList("col-is-host");
                
                row.Add(playerId);
                row.Add(playerName);
                row.Add(lastUpdated);
                row.Add(connectionStatus);
                row.Add(playerIsHost);
                
                _table.Add(row);
            }
            _table.style.height = playerCount * lobbyRowHeight;
        }
        
        public override void Update() {
            Debug.Log("Lobby calling update");
            _table.Clear();
            UpdateServerInfoTable();
            DisplayButtons();
            GenerateLobbyPlayerTable(true);
        }
        
        public override void RePaint() {
            Debug.Log("Lobby calling repaint");
            _table.Clear();
            UpdateServerInfoTable();
            DisplayButtons();
            GenerateLobbyPlayerTable(false);
        }

        private void ApplyServerStatusStyling() {
            foreach (var statusClass in ServerStatusClasses.Values){
                _serverStatusLabel.RemoveFromClassList(statusClass);
            }
            
            if (ServerStatusClasses.TryGetValue(_uiController.Client.ServerStatus, out var newClass)) {
                _serverStatusLabel.AddToClassList(newClass);
            }
            else {
                _serverStatusLabel.AddToClassList(ServerStatusClasses["Inactive"]);
            }

            _serverStatusLabel.text = Capitalize(_serverStatusLabel.text);
        }
        
        private string Capitalize(string input) {
            if (string.IsNullOrEmpty(input))
                return input;

            input = input.ToLower();
            return char.ToUpper(input[0]) + input.Substring(1);
        }
    }
}