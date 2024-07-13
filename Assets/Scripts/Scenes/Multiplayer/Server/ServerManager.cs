using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Multiplay;
using UnityEngine;

namespace Multiplayer {
    public sealed class ServerManager {

        #if DEDICATED_SERVER
        private IServerQueryHandler _serverQueryHandler;
        #endif

        private static ServerManager _instance = null;
        private static readonly object Padlock = new object();
        private Dictionary<ulong, PlayerData> _playerDataDictionary = new Dictionary<ulong, PlayerData>();
        
        public static ServerManager Instance {
            get {
                lock (Padlock) {
                    _instance ??= new ServerManager();
                    return _instance;
                }
            }
        }

        /// <summary>
        /// Initialize Unity Authentication
        /// Subscribe to multiplay server events
        /// </summary>
        public async Task InitializeUnityAuth() {
            if (UnityServices.State != ServicesInitializationState.Initialized) {
                var initializationOptions = new InitializationOptions();

                #if DEDICATED_SERVER
                    await UnityServices.InitializeAsync();
                    Debug.Log("DEDICATED_SERVER LOBBY - INITIALIZING");

                    var multiplayEventCallbacks = new MultiplayEventCallbacks();
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
                #if DEDICATED_SERVER
                    Debug.Log("DEDICATED_SERVER LOBBY - ALREADY INITIALIZED");

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
        private void OnAllocate(MultiplayAllocation allocation) {
            #if DEDICATED_SERVER
                Debug.Log("DEDICATED_SERVER OnAllocate");
                
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
            #endif
        }

        private void OnDeallocate(MultiplayDeallocation deallocation) {
            #if DEDICATED_SERVER

            #endif
        }

        private void OnError(MultiplayError error) {
            #if DEDICATED_SERVER

            #endif
        }

        private void OnSubscriptionStateChanged(MultiplayServerSubscriptionState stateChanged) {
            #if DEDICATED_SERVER

            #endif
        }

        /// <summary>
        /// Subscribe to the Network Manager event and start the server
        /// </summary>
        private async void StartServer() {
            #if DEDICATED_SERVER
                NetworkManager.Singleton.ConnectionApprovalCallback += OnConnectionApproval;
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
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
        /// Approve clients attempting to connect, store the client's player data
        /// </summary>
        /// <param name="request">Client connection request</param>
        /// <param name="response">Client connection response</param>
        private void OnConnectionApproval(NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response) {

            Debug.Log("Approving new client connection...ClientNetworkId: " + request.ClientNetworkId);

            response.Approved = true;
            response.CreatePlayerObject = true;

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
        }

        /// <summary>
        /// When a client connects, notify all other clients
        /// </summary>
        /// <param name="clientId">Client ID</param>
        private void OnClientConnected(ulong clientId) {
            Debug.Log($"New client connected: {clientId}");
            PlayerData playerData = _playerDataDictionary[clientId];
            NotifyClientsOfPlayerStatus(playerData, true);
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
        }
        
        /// <summary>
        /// Update the multiplay server to keep it alive
        /// </summary>
        public void UpdateServer() {
            #if DEDICATED_SERVER
                if (_serverQueryHandler != null) {
                    _serverQueryHandler.UpdateServerCheck();
                }
            #endif
        }
        
        /// <summary>
        /// Ntoify all other clients when a plyer connects or disconnects from the multiplay server
        /// </summary>
        /// <param name="playerData">The player's data</param>
        /// <param name="isConnected">If the player connected or disconnected</param>
        private void NotifyClientsOfPlayerStatus(PlayerData playerData, bool isConnected) {
            PlayerStatusMessage msg = new PlayerStatusMessage {
                PlayerId = playerData.PlayerId,
                IsConnected = isConnected
            };

            foreach (var client in NetworkManager.Singleton.ConnectedClientsList) {
                Debug.Log("Sending connection event notification to client: " + client.ClientId + " with playerID: " + msg.PlayerId);
                NetworkManager.Singleton.SendCustomMessageToClient(client.ClientId, msg);
            }
        }
        
        /// <summary>
        /// When the host attempts to disconnect, send a message to notify all other clients
        /// </summary>
        /// <param name="clientId">Client ID</param>
        /// <param name="reader">reader</param>
        private async void OnHostLeavingMessageReceived(ulong clientId, FastBufferReader reader) {
            #if DEDICATED_SERVER
                foreach (var client in NetworkManager.Singleton.ConnectedClientsList) {
                    Debug.Log("Sending host leaving notification to client: " + client.ClientId);
                    NetworkManager.Singleton.SendHostLeavingMessageToClient(client.ClientId, new HostLeavingMessage());
                }
            #endif
        }
    }
}