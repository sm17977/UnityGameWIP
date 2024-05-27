using System.Threading.Tasks;
using UnityEngine.InputSystem.HID;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    public class LobbyView : View {
        
        private MultiplayerUIController _uiController;
        
        private VisualElement _table;
        private Button _startGameBtn;
        private Button _joinGameBtn;
        private Button _leaveBtn;

        private VisualElement _backBtnContainer;
        private Button _backBtn;    
    
        private VisualElement _serverInfoTable;
        private Label _serverStatusLabel;
        private Label _serverIPLabel;
        private Label _serverPortLabel;
        
        private string _serverStatus = "N/A";
        private string _serverIP;
        private string _serverPort;
        private string _playerConnectionStatus = "Not Connected";
        
        public LobbyView(VisualElement parentContainer, MultiplayerUIController uiController) {
            _uiController = uiController;
            ParentContainer = parentContainer;
            var uiDocument = uiController.uiDocument;
            Root = uiDocument.rootVisualElement;
            InitializeElements();
        }

        private void InitializeElements() {
            
            _serverStatusLabel = Root.Q<Label>("server-status-label");
            _serverIPLabel = Root.Q<Label>("server-ip-label");
            _serverPortLabel = Root.Q<Label>("server-port-label");
            
            _table = Root.Q<VisualElement>("current-lobby-table-body");
            _leaveBtn = Root.Q<Button>("leave-lobby-btn");
            _startGameBtn = Root.Q<Button>("start-game-btn");
            _joinGameBtn = Root.Q<Button>("join-game-btn");

            _backBtnContainer = Root.Q<VisualElement>("back-btn-container");
            _backBtn = Root.Q<Button>("back-btn");
            
            _startGameBtn.RegisterCallback<ClickEvent>(evt => OnClickStartGameBtn());
            _joinGameBtn.RegisterCallback<ClickEvent>(evt => OnClickJoinGameBtn());
            _leaveBtn.RegisterCallback<ClickEvent>( evt => OnClickLeaveLobbyBtn());
            _backBtn.RegisterCallback<ClickEvent>(evt => OnClickBackBtn());
        }

        public override async void Show() {
            base.Show();
            Show(_backBtnContainer);
            DisplayButtons();
            _table.Clear();
            await GenerateLobbyPlayerTable(false);
        }

        public override void Hide() {
            base.Hide();
            _table.style.maxHeight = 0;
            Hide(_backBtnContainer);
        }

        public void OnClickBackBtn() {
            _uiController.ReturnToMultiplayerMenu();
        }
        private async void OnClickStartGameBtn() {
            _startGameBtn.SetEnabled(false);
            bool clientConnected = await _uiController.StartGame();
            _startGameBtn.SetEnabled(true);

            if (clientConnected) {
                //hide main UI
            }
        }
        private async void OnClickJoinGameBtn() {
            _joinGameBtn.SetEnabled(false);
            bool clientConnected = await _uiController.JoinGame();
            _joinGameBtn.SetEnabled(true);

            if (clientConnected) {
                //hide main UI
            }
        }

        private async void OnClickLeaveLobbyBtn() {
            await _uiController.LeaveLobby();
        }
        
        private void DisplayButtons() {
            var isPlayerHost = _uiController.IsPlayerHost();
            if (isPlayerHost) {
                Show(_startGameBtn);
                Hide(_joinGameBtn);
            }
            else {
                Show(_joinGameBtn);
                Hide(_startGameBtn);
            }
        }

        private async Task UpdateServerInfoTable() {
            // if (!gameLobbyManager.IsPlayerHost()) {
            //     _serverStatusLabel.text = gameLobbyManager?.GetLobbyData("MachineStatus");
            //     _serverPortLabel.text =  gameLobbyManager?.GetLobbyData("Port");
            //     _serverIPLabel.text =  gameLobbyManager?.GetLobbyData("ServerIP");
            // }

            _serverStatusLabel.text = _uiController._serverStatus;
            _serverPortLabel.text = _uiController._serverPort;
            _serverIPLabel.text = _uiController._serverIP;
        }

        private async Task GenerateLobbyPlayerTable(bool sendNewRequest) {

            var lobbyPlayers = await _uiController.GetLobbyPlayerTableData(sendNewRequest);
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
                playerNameLabel.text = lobbyPlayer.Data["Name"].Value;
                playerName.Add(playerNameLabel);
                playerName.AddToClassList("col-player-name");
                
                VisualElement lastUpdated = new VisualElement();
                Label lastUpdatedLabel = new Label();
                lastUpdatedLabel.text = lobbyPlayer.Data["ClientId"].Value;
                lastUpdated.Add(lastUpdatedLabel);
                lastUpdated.AddToClassList("col-last-updated");
                
                VisualElement connectionStatus = new VisualElement();
                Label connectionStatusLabel = new Label();
                connectionStatusLabel.text = lobbyPlayer.Data["IsConnected"].Value;
                connectionStatus.Add(connectionStatusLabel);
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
            _table.style.maxHeight = playerCount * lobbyRowHeight;
        }
        
        public override void Update() {
            _table.Clear();
            UpdateServerInfoTable();
            GenerateLobbyPlayerTable(true);
        }
        
        public override void RePaint() {
            _table.Clear();
            UpdateServerInfoTable();
            GenerateLobbyPlayerTable(false);
        }
    }
}