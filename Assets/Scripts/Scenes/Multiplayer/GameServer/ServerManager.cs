#if UNITY_SERVER
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Multiplay;
using UnityEngine;
using Unity.Services.Multiplayer;


namespace Multiplayer {
    public sealed class ServerManager {

#if UNITY_SERVER
            private IServerQueryHandler _serverQueryHandler;
#endif

        private static ServerManager _instance = null;
        private static readonly object Padlock = new object();
        private Dictionary<ulong, PlayerData> _playerDataDictionary = new Dictionary<ulong, PlayerData>();
        private GameObject _spawnPointsParent;
        private List<Transform> _spawnPointsList;
        private IMultiplayerService _iMultiplayerService;

        public static ServerManager Instance {
            get {
                lock (Padlock) {
                    _instance ??= new ServerManager();
                    return _instance;
                }
            }
        }

        public void Initialize(GameObject spawnPointsParent) {
            _spawnPointsList = new List<Transform>();
            _spawnPointsParent = spawnPointsParent;
            foreach (Transform child in _spawnPointsParent.transform) {
                _spawnPointsList.Add(child);
            }
        }

        /// <summary>
        /// Initialize Unity Authentication
        /// Subscribe to multiplay server events
        /// </summary>
        public async Task InitializeUnityAuth() {
            if (UnityServices.State != ServicesInitializationState.Initialized) {
                var initializationOptions = new InitializationOptions();

#if UNITY_SERVER
                    await UnityServices.InitializeAsync();
                    
                    
                    Debug.Log("UNITY_SERVER LOBBY - INITIALIZING");

                    MultiplayEventCallbacks multiplayEventCallbacks = new();
                    multiplayEventCallbacks.Allocate += OnAllocate;
                    multiplayEventCallbacks.Deallocate += OnDeallocate;
                    multiplayEventCallbacks.Error += OnError;
                    multiplayEventCallbacks.SubscriptionStateChanged += OnSubscriptionStateChanged;
                    
                    var serverEvents =
     await MultiplayService.Instance.SubscribeToServerEventsAsync(multiplayEventCallbacks);

                    _serverQueryHandler =
                        await MultiplayService.Instance.StartServerQueryHandlerAsync((ushort)4, "MyServerName", "Arena", "89133",
                            "map");

                    var serverConfig = MultiplayService.Instance.ServerConfig;
                    if (serverConfig.AllocationId != "") {
                        OnAllocate(new MultiplayAllocation("", serverConfig.ServerId,
                            serverConfig.AllocationId));
                    }
#endif
            }
            else {
#if UNITY_SERVER
                    Debug.Log("UNITY_SERVER LOBBY - ALREADY INITIALIZED");

                    var serverConfig = MultiplayService.Instance.ServerConfig;
                    if (serverConfig.AllocationId != "") {
                        OnAllocate(new MultiplayAllocation("", serverConfig.ServerId,
                            serverConfig.AllocationId));
                    }
#endif
            }
        }

        /// <summary>
        /// When a server is allocated, set the Network Manager's connection data and start the server
        /// Register a custom messaging manager to notify when the lobby host leaves
        /// </summary>

#if UNITY_SERVER
        private void OnAllocate(MultiplayAllocation allocation) {
           
                Debug.Log("UNITY_SERVER OnAllocate");
                
                var serverConfig = MultiplayService.Instance.ServerConfig;
                Debug.Log($"Server ID[{serverConfig.ServerId}]");
                Debug.Log($"AllocationID[{serverConfig.AllocationId}]");
                Debug.Log($"Port[{serverConfig.Port}]");
                Debug.Log($"QueryPort[{serverConfig.QueryPort}");
                Debug.Log($"LogDirectory[{serverConfig.ServerLogDirectory}]");
                
                var port = serverConfig.Port;
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData("0.0.0.0", port, "0.0.0.0");
                StartServer();
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("HostLeaving", OnHostLeavingMessageReceived);
          
        }
#endif

#if UNITY_SERVER
        private void OnDeallocate(MultiplayDeallocation deallocation) {
            

        
        }
#endif

#if UNITY_SERVER
        private void OnError(MultiplayError error) {
           

         
        }
#endif

#if UNITY_SERVER
        private void OnSubscriptionStateChanged(MultiplayServerSubscriptionState stateChanged) {
            

        
        }
#endif

        /// <summary>
        /// Subscribe to the Network Manager event and start the server
        /// </summary>
        private async void StartServer() {
#if UNITY_SERVER
                    NetworkManager.Singleton.ConnectionApprovalCallback -= OnConnectionApproval;
                    NetworkManager.Singleton.ConnectionApprovalCallback += OnConnectionApproval;
                    
                    NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                    NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                    
                    NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
                    NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
                    
                    NetworkManager.Singleton.StartServer();
                    await MultiplayService.Instance.ReadyServerForPlayersAsync();
#endif
        }


        /// <summary>
        /// Disconnect the server
        /// </summary>
        public void Disconnect() {
            NetworkManager.Singleton.Shutdown();
        }

        /// <summary>
        /// Approve clients attempting to connect, set the spawn position
        /// and store the client's player data
        /// </summary>
        /// <param name="request">Client connection request</param>
        /// <param name="response">Client connection response</param>
        private void OnConnectionApproval(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response) {
#if UNITY_SERVER

                Debug.Log("Approving new client connection... ClientNetworkId: " + request.ClientNetworkId);
                
                response.CreatePlayerObject = true;
                response.Position = GetSpawnPoint();
                response.Approved = true;

                Debug.Log("response.Position: " + response.Position);

                PlayerData playerData;
                using (var reader = new FastBufferReader(request.Payload, Allocator.Temp)) {
                    try {
                        reader.ReadNetworkSerializable(out playerData);
                    }
                    catch (Exception ex) {
                        Debug.LogError($"Error reading player data: {ex.Message}");
                        return;
                    }
                }

                _playerDataDictionary[request.ClientNetworkId] = playerData;
                response.Pending = false;
#endif
        }

        /// <summary>
        /// When a client connects, notify all other clients
        /// </summary>
        /// <param name="clientId">Client ID</param>
        private void OnClientConnected(ulong clientId) {
            Debug.Log($"New client connected: {clientId}");
            //SpawnPlayer(clientId);
            PlayerData playerData = _playerDataDictionary[clientId];
            NotifyClientsOfPlayerStatus(playerData, true);
            if (NetworkBuffManager.Instance == null) {
                Debug.Log("NetworkBuffManager is null!");
            }

            NetworkBuffManager.Instance.AddPlayerToBuffStore(clientId);
        }

        /// <summary>
        /// When a client disconnects, notify all other clients
        /// </summary>
        /// <param name="clientId">Client ID</param>
        private void OnClientDisconnected(ulong clientId) {
            Debug.Log($"Client disconnected: {clientId}");
            PlayerData playerData = _playerDataDictionary[clientId];
            NotifyClientsOfPlayerStatus(playerData, false);
            _playerDataDictionary.Remove(clientId);
            NetworkBuffManager.Instance.RemovePlayerFromBuffStore(clientId);
        }

        /// <summary>
        /// Update the multiplay server to keep it alive
        /// </summary>
        public void UpdateServer() {
#if UNITY_SERVER
                if (_serverQueryHandler != null) {
                    _serverQueryHandler.UpdateServerCheck();
                }
#endif
        }

        /// <summary>
        /// Notify all other clients when a player connects or disconnects from the multiplay server
        /// </summary>
        /// <param name="playerData">The player's data</param>
        /// <param name="isConnected">If the player connected or disconnected</param>
        private void NotifyClientsOfPlayerStatus(PlayerData playerData, bool isConnected) {
            PlayerStatusMessage msg = new PlayerStatusMessage {
                PlayerId = playerData.PlayerId,
                IsConnected = isConnected
            };

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList) {
                Debug.Log("Sending connection event notification to client: " + client.ClientId + " with playerID: " +
                          msg.PlayerId);
                NetworkManager.Singleton.SendCustomMessageToClient(client.ClientId, msg);
            }
        }

        /// <summary>
        /// When the host attempts to disconnect, send a message to notify all other clients
        /// </summary>
        /// <param name="clientId">Client ID</param>
        /// <param name="reader">reader</param>
        private async void OnHostLeavingMessageReceived(ulong clientId, FastBufferReader reader) {
#if UNITY_SERVER
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList) {
                    Debug.Log("Sending host leaving notification to client: " + client.ClientId);
                    NetworkManager.Singleton.SendHostLeavingMessageToClient(client.ClientId, new HostLeavingMessage());
                }
#endif
        }

        private Vector3 GetSpawnPoint() {

            if (_spawnPointsList.Count == 0) {
                Initialize(_spawnPointsParent);
            }

            var spawnPoint = _spawnPointsList[0];
            _spawnPointsList.RemoveAt(0);
            Debug.Log("Spawn Pos: " + spawnPoint.position);
            return spawnPoint.position;
        }
    }
}
#endif