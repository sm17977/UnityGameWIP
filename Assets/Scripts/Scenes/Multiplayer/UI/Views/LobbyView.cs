using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UIElements;

namespace Multiplayer.UI {
    public delegate Task OnLeaveLobby();
    public delegate bool OnIsThisPlayerHost();
    public delegate bool OnIsPlayerHost(string playerId);
    public delegate Task<List<Player>> OnGetLobbyPlayerTableData(bool sendNewRequest);
    public delegate string OnGetLobbyPlayerId();
    public delegate string OnGetLobbyGameMode();
    public delegate string OnGetLobbyName();
    
    public class LobbyView : View {
        public event OnLeaveLobby LeaveLobby;
        public event OnIsThisPlayerHost IsThisPlayerHost;
        public event OnIsPlayerHost IsPlayerHost;
        public event OnGetLobbyPlayerTableData GetLobbyPlayerTableData;
        public event OnGetLobbyPlayerId GetLobbyPlayerId;
        public event OnGetLobbyGameMode GetLobbyGameMode;
        public event OnGetLobbyName GetLobbyName;
        
        private VisualElement _playersTableA;
        private VisualElement _playersTableB;
        private Label _lobbyName;
        private Label _playerId;
        private Label _gameModeName;
        private Label _gameModePlayerCount;
        private Button _leaveLobbyBtn;
        private Button _backBtn;    
        
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
                { MachineStatus.ShuttingDown,
                    new Dictionary<string, string>() {
                        {"className", "server-status-default"},
                        {"text", "Shutting Down"}
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
        
        protected LobbyView(VisualElement parentContainer, VisualTreeAsset vta) {
            Template = vta.Instantiate().Children().FirstOrDefault();
            ParentContainer = parentContainer;
            BindUIElements();
        }

        private void BindUIElements() {
            
            // Lobby Players Table
            _playersTableA = Template.Q<VisualElement>("lobby-table-body-a");
            _playersTableB = Template.Q<VisualElement>("lobby-table-body-b");
            
            // Text Labels
            _lobbyName = Template.Q<Label>("lobby-name-label");
            _playerId = Template.Q<Label>("player-id-text");
            _gameModeName = Template.Q<Label>("gamemode-name");
            _gameModePlayerCount = Template.Q<Label>("gamemode-player-count");
            
            // Buttons 
            _leaveLobbyBtn = Template.Q<Button>("back-btn");
            _leaveLobbyBtn.RegisterCallback<ClickEvent>( (evt) => { 
               var _= LeaveLobby?.Invoke();
            });
        }

        private void SetTextLabels() {
            _playerId.text = GetLobbyPlayerId?.Invoke();
            var gameModeName = GetLobbyGameMode?.Invoke();
            var gameMode = GlobalState.GameModeManager.GetGameMode(gameModeName);
            var playersPerTeam = gameMode.MinimumRequiredPlayers/2;
            _gameModeName.text = gameModeName;
            _gameModePlayerCount.text = playersPerTeam + "v" + playersPerTeam;
            _lobbyName.text = GetLobbyName?.Invoke();
        }   

        public override async void Show() {
            base.Show();
            SetTextLabels();
            await GenerateLobbyPlayerTable(false);
        }

        public override void Hide() {
            base.Hide();
            _playersTableA.style.height = 0;
            _playersTableB.style.height = 0;
        }
        
        /// <summary>
        /// Populates the lobby player table
        /// </summary>
        /// <param name="sendNewRequest">If true, request new lobby data from the Lobby service</param>
        private async Task GenerateLobbyPlayerTable(bool sendNewRequest) {
            _playersTableA.Clear();
            _playersTableB.Clear();
            var lobbyPlayers = await GetLobbyPlayerTableData.Invoke(sendNewRequest);
            if (lobbyPlayers == null) return;
            
            var playerCount = 0;
            var lobbyRowHeight = 61;
            var countTeamA = 0;
            var countTeamB = 0;

            foreach (var lobbyPlayer in lobbyPlayers) {

                var tableA = playerCount % 2 == 0;
                playerCount++;
                
                VisualElement row = new VisualElement();
                row.AddToClassList("table-row");
                
                // Player ID
                Label playerNameLabel = new Label();
                playerNameLabel.text = lobbyPlayer.Data["Name"].Value;
                
                // Player indicator
                var playerId = GetLobbyPlayerId?.Invoke();
                if (playerId != null && playerId == lobbyPlayer.Id) {
                    playerNameLabel.AddToClassList("player-name-highlight");
                }
                row.Add(playerNameLabel);
                
                // Player ready indicator
                var playerReady = bool.Parse(lobbyPlayer.Data["IsReady"].Value);
                if (playerReady) {
                    Label playerReadyLabel = new Label();
                    playerReadyLabel.text = "Ready";
                    playerReadyLabel.AddToClassList("player-ready-indicator");
                    row.Add(playerReadyLabel);
                }
                
                // Host indicator
                if (IsPlayerHost.Invoke(lobbyPlayer.Id)) {
                    Label hostLabel = new Label();
                    hostLabel.text = "H";
                    hostLabel.AddToClassList("player-host-indicator");
                    row.Add(hostLabel);
                }
                
                if (tableA) {
                    countTeamA++;
                    _playersTableA.Add(row);
                }
                else {
                    countTeamB++;
                    _playersTableB.Add(row);
                }
            }
            
            // Delay setting the height of the table to ensure the USS transition is triggered
            // The transition is set on the height of the table body, so when the table is generated,
            // it unravels downwards showing each entry
            _playersTableA.schedule.Execute(() => {
                _playersTableA.style.height = countTeamA * lobbyRowHeight;
            }).StartingIn(50);
            
            _playersTableB.schedule.Execute(() => {
                _playersTableB.style.height = countTeamB * lobbyRowHeight;
            }).StartingIn(50);
        }
        
        /// <summary>
        /// Update the table with the most recent lobby data
        /// This sends a request to the Lobby service so only call this when necessary
        /// </summary>
        public override async void Update() {
            Debug.Log("Lobby calling update");
            await GenerateLobbyPlayerTable(true);
        }
        
        /// <summary>
        /// Repaint the table with the local lobby data
        /// </summary>
        public override async void RePaint() {
            Debug.Log("Lobby calling repaint");
            await GenerateLobbyPlayerTable(false);
        }
    }
}