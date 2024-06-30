using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    public delegate Task<bool> OnStartGame();
    public delegate Task<bool> OnJoinGame();
    public delegate Task OnLeaveLobby();
    public delegate bool OnIsThisPlayerHost();
    public delegate bool OnIsPlayerHost(string playerId);
    public delegate bool OnCanStartGame();
    public delegate bool OnCanJoinGame();
    public delegate Task<List<Player>> OnGetLobbyPlayerTableData(bool sendNewRequest);
    
    
    public class LobbyView : View {

        public event OnStartGame StartGame;
        public event OnJoinGame JoinGame;
        public event OnLeaveLobby LeaveLobby;
        public event OnIsThisPlayerHost IsThisPlayerHost;
        public event OnIsPlayerHost IsPlayerHost;
        public event OnCanStartGame CanStartGame;
        public event OnCanJoinGame CanJoinGame;
        public event OnGetLobbyPlayerTableData GetLobbyPlayerTableData;
        

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
            _leaveBtn.RegisterCallback<ClickEvent>( (evt) => { 
               var _= LeaveLobby?.Invoke();
            });
            _backBtn.RegisterCallback<ClickEvent>(evt => OnReturnToMultiplayerMenu());
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
        
        private async void OnClickStartGameBtn() {
            _startGameBtn.SetEnabled(false);
            var clientConnected = await StartGame?.Invoke();
            
            if (clientConnected) {
            }
        }
        private async void OnClickJoinGameBtn() {
            _joinGameBtn.SetEnabled(false);
            var clientConnected = await JoinGame?.Invoke();
            
            if (clientConnected) {
                _joinGameBtn.SetEnabled(true);
            }
        }
        
        private void DisplayButtons() {
            var isPlayerHost = IsThisPlayerHost?.Invoke() == true;
            if (isPlayerHost) {
                _startGameBtn.SetEnabled(CanStartGame?.Invoke() == true);
                Show(_startGameBtn);
                Hide(_joinGameBtn);
            }
            else {
                _joinGameBtn.SetEnabled(CanJoinGame?.Invoke() == true);
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

            var lobbyPlayers = await GetLobbyPlayerTableData.Invoke(sendNewRequest);
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
                playerIsHostLabel.text = IsPlayerHost.Invoke(lobbyPlayer.Id) ? "Yes" : "No";
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
        
        public override async void Update() {
            Debug.Log("Lobby calling update");
            _table.Clear();
            UpdateServerInfoTable();
            DisplayButtons();
            await GenerateLobbyPlayerTable(true);
        }
        
        public override async void RePaint() {
            Debug.Log("Lobby calling repaint");
            _table.Clear();
            UpdateServerInfoTable();
            DisplayButtons();
            await GenerateLobbyPlayerTable(false);
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