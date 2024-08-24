using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    public delegate Task OnStartGame();
    public delegate Task OnStartServer();
    public delegate Task<bool> OnJoinGame();
    public delegate Task OnLeaveLobby();
    public delegate bool OnIsThisPlayerHost();
    public delegate bool OnIsPlayerHost(string playerId);
    public delegate bool OnCanStartGame();
    public delegate bool OnCanStartServer();
    public delegate bool OnCanJoinGame();
    public delegate Task<List<Player>> OnGetLobbyPlayerTableData(bool sendNewRequest);
    public delegate Task OnReadyUp();

    
    public class LobbyView : View {

        public event OnStartGame StartGame;
        public event OnStartServer StartServer;
        public event OnJoinGame JoinGame;
        public event OnLeaveLobby LeaveLobby;
        public event OnIsThisPlayerHost IsThisPlayerHost;
        public event OnIsPlayerHost IsPlayerHost;
        public event OnCanStartGame CanStartGame;
        public event OnCanStartServer CanStartServer;
        public event OnCanJoinGame CanJoinGame;
        public event OnGetLobbyPlayerTableData GetLobbyPlayerTableData;
        public event OnReadyUp ReadyUp;
     
        private VisualElement _table;
        private Button _startGameBtn;
        private Button _joinGameBtn;
        private Button _leaveLobbyBtn;
        private Button _readyUpBtn;
        private Button _startServerBtn;
        private Button _backBtn;    
    
        private VisualElement _serverInfoTable;
        private Label _serverStatusLabel;
        private Label _serverIPLabel;
        private Label _serverPortLabel;

        private string _serverStatus;
        private string _serverIP;
        private string _serverPort;
        private string _playerConnectionStatus;
        
        // Excluding "INACTIVE", these are all the possible multi-play machine statuses
        // They are assigned a USS class name for styling and a text value for the UI label
        private static readonly Dictionary<string, Dictionary<string, string>> ServerStatusTypes =
            new(){
                { MachineStatus.Shutdown,
                    new Dictionary<string, string>() {
                        {"className", "server-status-default"},
                        {"text", "Shutdown"}
                    }
                },
                { MachineStatus.Booting,
                    new Dictionary<string, string>() {
                        {"className", "server-status-booting"},
                        {"text", "Booting"}
                    }
                },
                { MachineStatus.AwaitingSetup,
                    new Dictionary<string, string>() {
                        {"className", "server-status-awaiting-setup"},
                        {"text", "Awaiting Setup"}
                    }
                },
                { MachineStatus.Online,
                    new Dictionary<string, string>() {
                        {"className", "server-status-online"},
                        {"text", "Online"}
                    }
                },
                { "INACTIVE",
                    new Dictionary<string, string>() {
                        {"className", "server-status-default"},
                        {"text", "Inactive"}
                    }
                },
            };
        
        public LobbyView(VisualElement parentContainer, VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();
            ParentContainer = parentContainer;
            InitializeElements();
        }

        private void InitializeElements() {
            _serverStatusLabel = Template.Q<Label>("server-status-label");
            _serverIPLabel = Template.Q<Label>("server-ip-label");
            _serverPortLabel = Template.Q<Label>("server-port-label");
            
            _table = Template.Q<VisualElement>("current-lobby-table-body");
            
            _leaveLobbyBtn = Template.Q<Button>("leave-lobby-btn");
            _startGameBtn = Template.Q<Button>("start-game-btn");
            _joinGameBtn = Template.Q<Button>("join-game-btn");
            _readyUpBtn = Template.Q<Button>("ready-game-btn");
            _startServerBtn = Template.Q<Button>("start-server-btn");
            
            _backBtn = Template.Q<Button>("back-btn");
            
            _startGameBtn.RegisterCallback<ClickEvent>(evt => OnClickStartGameBtn());
            _joinGameBtn.RegisterCallback<ClickEvent>(evt => OnClickJoinGameBtn());
            _leaveLobbyBtn.RegisterCallback<ClickEvent>( (evt) => { 
               var _= LeaveLobby?.Invoke();
            });
            _readyUpBtn.RegisterCallback<ClickEvent>((evt) => ReadyUp?.Invoke());
            _startServerBtn.RegisterCallback<ClickEvent>(evt => OnClickStartServerBtn());
            
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

        private async void OnClickStartServerBtn() {
            await StartServer?.Invoke();
            RePaint();
        }
        
        private async void OnClickStartGameBtn() {
            _startGameBtn.SetEnabled(false);
            await StartGame?.Invoke();
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
                _startServerBtn.SetEnabled(CanStartServer?.Invoke() == true);
                Show(_startGameBtn);
                Show(_startServerBtn);
                Hide(_joinGameBtn);
                Hide(_readyUpBtn);
            }
            else {
                _joinGameBtn.SetEnabled(CanJoinGame?.Invoke() == true);
                Show(_joinGameBtn);
                Show(_readyUpBtn);
                Hide(_startGameBtn);
                Hide(_startServerBtn);
            }
        }
        
        public void UpdateServerInfoTable(Client client) {
            
            var rawStatus = client.ServerStatus;
            _serverStatusLabel.text = ServerStatusTypes[rawStatus]["text"];
            _serverPortLabel.text = client.Port;
            _serverIPLabel.text = client.ServerIP;
            
            if (!String.IsNullOrEmpty(client.ServerStatus)) {
                ApplyServerStatusStyling(client.ServerStatus);
            }
        }

        private async Task GenerateLobbyPlayerTable(bool sendNewRequest) {

            var lobbyPlayers = await GetLobbyPlayerTableData.Invoke(sendNewRequest);
            if (lobbyPlayers == null) return;
            
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
            DisplayButtons();
            await GenerateLobbyPlayerTable(true);
        }
        
        public override async void RePaint() {
            Debug.Log("Lobby calling repaint");
            _table.Clear();
            DisplayButtons();
            await GenerateLobbyPlayerTable(false);
        }

        private void ApplyServerStatusStyling(string serverStatus) {
            foreach (var statusClass in ServerStatusTypes.Values){
                _serverStatusLabel.RemoveFromClassList(statusClass["className"]);
            }
            
            if (ServerStatusTypes.TryGetValue(serverStatus, out var newClass)) {
                _serverStatusLabel.AddToClassList(newClass["className"]);
            }
            else {
                _serverStatusLabel.AddToClassList(ServerStatusTypes["INACTIVE"]["className"]);
            }
        }
    }
}