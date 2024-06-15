
using System;
using System.Collections;
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

        private ServerManager() {
        }

        public static ServerManager Instance {
            get {
                lock (Padlock) {
                    _instance ??= new ServerManager();
                    return _instance;
                }
            }
        }

        public async Task InitializeUnityAuth() {
            if (UnityServices.State != ServicesInitializationState.Initialized) {
                Debug.Log("TEST");
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
                    await MultiplayService.Instance.StartServerQueryHandlerAsync((ushort)4, "MyServerName", "Arena", "85662",
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

        private async void StartServer() {
#if DEDICATED_SERVER
            NetworkManager.Singleton.ConnectionApprovalCallback += OnConnectionApproval;
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            NetworkManager.Singleton.StartServer();
            await MultiplayService.Instance.ReadyServerForPlayersAsync();
#endif
        }

        public void Disconnect() {
            NetworkManager.Singleton.Shutdown();
        }


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

        private void OnClientConnected(ulong clientId) {
            Debug.Log($"New client connected: {clientId}");
            PlayerData playerData = _playerDataDictionary[clientId];
            NotifyClientsOfPlayerStatus(playerData, true);
        }

        public void UpdateServer() {
#if DEDICATED_SERVER
            if (_serverQueryHandler != null) {
                _serverQueryHandler.UpdateServerCheck();
            }
#endif
        }

        private void OnClientDisconnected(ulong clientId) {
            Debug.Log($"Client disconnected: {clientId}");
            PlayerData playerData = _playerDataDictionary[clientId];
            NotifyClientsOfPlayerStatus(playerData, false);
            _playerDataDictionary.Remove(clientId);
        }

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