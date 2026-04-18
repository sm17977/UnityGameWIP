using System;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using Scenes.Multiplayer.EdgeGapAPI;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Multiplayer {
    public delegate void OnUpdateServerData(Client client); 
    public delegate void OnRePaintLobbyView(); 
    public delegate void OnHostDisconnection(); 
    public class ClientManager : MonoBehaviour {
        
        public event OnUpdateServerData UpdateServerData;
        public event OnRePaintLobbyView RePaintLobbyView;
        public event OnHostDisconnection HostDisconnection;

        public static ClientManager Instance;
        public Client Client;
        private CancellationTokenSource _cancellationTokenSource;
        private WebServicesAPI _webServicesAPI;
        private GameLobbyManager _gameLobbyManager;
        private EdgeGapClient _edgeGapApi;
        
        private void Awake() {
            
            #if UNITY_SERVER
                gameObject.SetActive(false);
                return;
            #endif
            
            if(Instance == null){
                Instance = this;
            }
            else if(Instance != this){
                Destroy(this);
            }
            
            Client = Client.Instance;
            _gameLobbyManager = GameLobbyManager.Instance;
            _webServicesAPI = new WebServicesAPI();
            _edgeGapApi = new EdgeGapClient();
        }

        private void Start() {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        
        /// <summary>
        /// Deploy a new EdgeGap server 
        /// </summary>
        public async Task StartServer() {
            _cancellationTokenSource = new CancellationTokenSource();
            if (Client.IsLobbyHost) {
                await CreateServer();
            }
        }

        /// <summary>
        /// Disconnect the client from the EdgeGap server
        /// Update the player's connection status
        /// </summary>
        public async Task Disconnect() {
            if (NetworkManager.Singleton != null) {
                NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("PlayerStatusMessage");
                NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("HostLeaving");
                NetworkManager.Singleton.Shutdown();
            }

            Client.IsConnectedToServer = false;
            await _gameLobbyManager.UpdatePlayerDataWithConnectionStatus(Client.IsConnectedToServer);
        }


        /// <summary>
        ///  Deploys an EdgeGap server and updates lobby clients with connection info
        /// </summary>
        private async Task CreateServer() {

            try {
                // Send the deployment request
                var requestId = await InitDeployment();
                GetDeploymentResponse activeDeployment = null;

                // Poll the deployment till the server is running, should take less than 2 mins usually
                if (!string.IsNullOrEmpty(requestId)) {
                    var deployment = await GetDeployment(requestId);
                    if (!deployment.running) {
                        activeDeployment = await PollForDeployment(requestId, 60 * 5);
                    }
                }

                // Once the server is running, we get the necessary connection info for clients to join
                if (activeDeployment != null) {
                    var host = activeDeployment.fqdn;
                    var port = activeDeployment.ports.gameport.external.ToString(); 

                    _gameLobbyManager.currentServerProvisionState = ServerProvisionState.Provisioned;
                    UpdateServerInfoForLobbyHost(host, port);
                    await _gameLobbyManager.UpdateLobbyWithServerInfo(Client.ServerStatus, host, port);
                }
            }
            catch (OperationCanceledException) {
                _gameLobbyManager.currentServerProvisionState = ServerProvisionState.Failed;
            }
        }

        /// <summary>
        /// Sends the deployment request
        /// </summary>
        /// <returns></returns>
        private async Task<string> InitDeployment() {
            
            var requestId = "";
            
            // Get the lobby host's IP to pass into the EdgeGap deployment request
            var ipAddress = await _edgeGapApi.GetIPAddress();
            
            var users = new[]
            {
                new User
                {
                    user_type = "ip_address",
                    user_data = new UserData
                    {
                        ip_address = ipAddress
                    }
                }
            };
            
            try {
                requestId = await _edgeGapApi.Deploy(users);
                Client.RequestId = requestId;
            }
            catch (Exception ex) {
                Debug.Log("Error trying to deploy: " + ex.Message);
            }

            return requestId;
        }

        private async Task<GetDeploymentResponse> GetDeployment(string requestId) {
            var deployment = await _edgeGapApi.GetDeploymentStatus(requestId);
            return deployment;
        }

        private async Task<GetDeploymentResponse> PollForDeployment(string requestId, int timeout) {

            var elapsed = 0;
            const int pollInterval = 5;
            
            do {
                var deployment = await GetDeployment(requestId);
                var status = deployment.current_status;
                await _gameLobbyManager.UpdateLobbyWithServerInfo(status, "", "");
                UpdateServerStatusForLobbyHost(status);
                
                if (deployment.running) {
                    return deployment;
                }

                await Task.Delay(pollInterval * 1000);
                elapsed += pollInterval;

            } while (elapsed < timeout);

            return null;
        }
        
        /// <summary>
        /// Start client to join the game server
        /// Before connecting to the server, the client sends their playerId in the connection approval payload,
        /// the server will receive this before approving, this enables us to track who has connected both from
        /// the client and server.
        /// </summary>
        /// <returns>boolean</returns>
        public bool StartClient() {
            
            if (NetworkManager.Singleton.IsClient) return false;
            if (Client.ServerIP == null || Client.Port == null) return false;
            
            try {
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(Client.ServerIP, (ushort)int.Parse(Client.Port));
                SendPlayerIdWithConnectionRequest();
                NetworkManager.Singleton.StartClient();
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("PlayerStatusMessage",
                    OnPlayerStatusMessage);
                if (!Client.IsLobbyHost) {
                    NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("HostLeaving");
                    NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("HostLeaving", OnHostLeavingMessageReceived);
                }
                Client.IsConnectedToServer = true;
                return true;
            }
            catch (OperationCanceledException) {
                Debug.Log("error");
                Client.IsConnectedToServer = false;
                return false;
            }
        }
        
        /// <summary>
        /// Update the server status in the Lobby Host View 
        /// </summary>
        /// <param name="status">Server Status</param>
        private void UpdateServerStatusForLobbyHost(string status) {
            Client.ServerStatus = status;
            if (status == "") {
                Client.ServerStatus = "INACTIVE";
            }
            UpdateServerData?.Invoke(Client);
        }

        /// <summary>
        /// Update the server info (IP and Port) in the Lobby Host View 
        /// </summary>
        /// <param name="ip">Server IP</param>
        /// <param name="port">Server Port</param>
        private void UpdateServerInfoForLobbyHost(string ip, string port) {
            Client.ServerIP = ip;
            Client.Port = port;
            UpdateServerData?.Invoke(Client);
        }

        /// <summary>
        /// Send playerId of the client to the server in the connection approval payload 
        /// </summary>
        private void SendPlayerIdWithConnectionRequest() {
            var playerData = new PlayerData { PlayerId = _gameLobbyManager.GetPlayerID() };
            using (var writer = new FastBufferWriter(128, Allocator.Temp)) {
                writer.WriteNetworkSerializable(playerData);
                NetworkManager.Singleton.NetworkConfig.ConnectionData = writer.ToArray();
            }
        }
        
        /// <summary>
        /// This function is called whenever a client connects to the server
        /// When called, the player updates the lobby with their connection status.
        /// </summary>
        /// <param name="clientId">Client ID</param>
        /// <param name="reader">FastBufferReader</param>
        private async void OnPlayerStatusMessage(ulong clientId, FastBufferReader reader) {
            PlayerStatusMessage msg = new PlayerStatusMessage();
            reader.ReadNetworkSerializable<PlayerStatusMessage>(out msg);
            Debug.Log("Player has connected/disconnected. Player Id:  " + msg.PlayerId + ", IsConnected: " + msg.IsConnected);
            Debug.Log("Updating player data...");
            if (msg.PlayerId == _gameLobbyManager.GetPlayerID()) {
                await _gameLobbyManager.UpdatePlayerDataWithConnectionStatus(msg.IsConnected);
                RePaintLobbyView?.Invoke();
            }
        }

        /// <summary>
        /// Process server initiated disconnect because lobby host has disconnected 
        /// </summary>
        private async void OnHostLeavingMessageReceived(ulong clientId, FastBufferReader reader) {
            if (!Client.IsLobbyHost) {
                await Disconnect();
                HostDisconnection?.Invoke();
            }
        }
        
        /// <summary>
        /// Send a custom message to the multiplay server notifying that the host has disconnected
        /// </summary>
        public void NotifyServerOfHostDisconnect() {
            NetworkManager.Singleton.SendHostLeavingMessageToServer(NetworkManager.ServerClientId, new HostLeavingMessage());
        }
        
        /// <summary>
        /// Log client disconnection message
        /// </summary>
        private void OnClientDisconnected(ulong clientId) {
            Debug.Log("Client disconnected (from server): " + clientId);
        }
        
        /// <summary>
        /// Remove any server allocations and stop the game server
        /// </summary>
        public async void StopServer() {
            if(Client.RequestId == null) return;
            await _edgeGapApi.StopDeployment(Client.RequestId);
            Client.ServerStatus = "REQUESTED_TERMINATION";
            ResetClient();
        }

        /// <summary>
        /// Reset the client instance by removing the server connection info
        /// </summary>
        public void ResetClient() {
            Client.ClearConnectionData();
        }

        private void OnApplicationQuit() {
            if (Client.IsLobbyHost && Client.ServerStatus != "REQUESTED_TERMINATION") {
                StopServer();
            }
        }
    }
}