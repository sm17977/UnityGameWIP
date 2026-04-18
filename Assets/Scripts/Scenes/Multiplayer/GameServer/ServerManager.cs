#if UNITY_SERVER
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using QFSW.QC;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using UnityEngine;
using Unity.Services.Multiplayer;


namespace Multiplayer {
    public sealed class ServerManager {

        #if UNITY_SERVER
            private bool _serverStarted;
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

                
                
                #endif
            }
        }

        /// <summary>
        /// When a server is allocated, set the Network Manager's connection data and start the server
        /// Register a custom messaging manager to notify when the lobby host leaves
        /// </summary>

        #if UNITY_SERVER
            private void OnAllocate( ) {

            }
        #endif

        #if UNITY_SERVER
            private void OnDeallocate( ) {}
        #endif

        #if UNITY_SERVER
            private void OnError( ) {}
        #endif

        #if UNITY_SERVER
            private void OnSubscriptionStateChanged( ) {}
        #endif

        /// <summary>
        /// Subscribe to the Network Manager event and start the server
        /// </summary>
        public void StartServer() {
            #if UNITY_SERVER
            
                    Debug.Log("Start Server t1");
            
                    NetworkManager.Singleton.ConnectionApprovalCallback -= OnConnectionApproval;
                    NetworkManager.Singleton.ConnectionApprovalCallback += OnConnectionApproval;
                    
                    NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
                    NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                    
                    NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
                    NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
                    
                    Debug.Log("Start Server t2");
                    
            
                    NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(
                        "0.0.0.0",  // The IP address is a string
                        (ushort)7777, // The port number is an unsigned short
                        "0.0.0.0" // The server listen address is a string.
                    );

                    Debug.Log("Start Server t3");
                    
                    var result = NetworkManager.Singleton.StartServer();
                    
                    Debug.Log("Start Server t4, result: " +  result);
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
            return spawnPoint.position;
        }
    }
}
#endif