using System;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using Multiplayer.UI;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Multiplayer {
    public class ClientManager : MonoBehaviour {

        public static ClientManager Instance;
        private Client _client;
        private CancellationTokenSource _cancellationTokenSource;
        
        private GameLobbyManager _gameLobbyManager;
        private ViewManager _viewManager;
        
        public Client Client {
            get {
                if (_client == null) {
                    _client = Client.Instance;
                }
                return _client;
            }
        }
        private void Awake() {
            
            #if DEDICATED_SERVER
                gameObject.SetActive(false);
                return;
            #endif
            
            if(Instance == null){
                Instance = this;
            }
            else if(Instance != this){
                Destroy(this);
            }
            
            _client = Client.Instance;
            _gameLobbyManager = GameLobbyManager.Instance;
            _viewManager = ViewManager.Instance;
        }

        private void Start() {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        
        /// <summary>
        /// Connect the client to the multiplay server
        /// </summary>
        /// <returns>boolean</returns>
        public async Task<bool> Connect() {
            _cancellationTokenSource = new CancellationTokenSource();

            if (_client.IsLobbyHost) {
                return await StartClientAsLobbyHost(_cancellationTokenSource.Token);
            }
         
            return StartClientAsLobbyPlayer();
        }

        /// <summary>
        /// Disconnect the client form the multiplay server
        /// Update the player's connection status
        /// </summary>
        public async Task Disconnect() {
            NetworkManager.Singleton.Shutdown();
            _client.IsConnectedToServer = false;
            await _gameLobbyManager.UpdatePlayerDataWithConnectionStatus(_client.IsConnectedToServer);
        }
        
        /// <summary>
        /// Start client as the lobby host.
        /// Queues a server allocation request and waits till server connection info has been returned.
        /// Once returned, updates the lobby with the connection info required to join the server.
        /// Before connecting to the server, the client sends their playerId in the connection approval payload,
        /// the server will receive this before approving, this enables us to track who has connected both from
        /// the client and server.
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>boolean</returns>
        private async Task<bool> StartClientAsLobbyHost(CancellationToken cancellationToken) {

            try {
                WebServicesAPI webServicesAPI = new WebServicesAPI();
                await webServicesAPI.RequestAPIToken();
                var clientConnectionInfo = await GetClientConnectionInfo(cancellationToken, webServicesAPI);
                
                if (clientConnectionInfo.IP != null || clientConnectionInfo.IP != "") {
                    
                    UpdateServerInfoForLobbyHost(clientConnectionInfo.IP, clientConnectionInfo.Port.ToString());
                    await _gameLobbyManager.UpdateLobbyWithServerInfo(_client.ServerStatus, clientConnectionInfo.IP, clientConnectionInfo.Port.ToString());
                    NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(clientConnectionInfo.IP, (ushort)clientConnectionInfo.Port);
                    SendPlayerIdWithConnectionRequest();
                    NetworkManager.Singleton.StartClient();
                    NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("PlayerStatusMessage",
                        OnPlayerStatusMessage);
                    _client.IsConnectedToServer = true;
                    return true;
                }
            }
            catch (OperationCanceledException) {
           
            }
            _client.IsConnectedToServer = false;
            return false;
        }
        
        /// <summary>
        /// Start client to join the game server as a player (a.k.a not the lobby host).
        /// Before connecting to the server, the client sends their playerId in the connection approval payload,
        /// the server will receive this before approving, this enables us to track who has connected both from
        /// the client and server.
        /// </summary>
        /// <returns>boolean</returns>
        private bool StartClientAsLobbyPlayer() {

            if (_client.ServerIP == null || _client.Port == null) return false;
       
            try {
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(_client.ServerIP, (ushort)int.Parse(_client.Port));
                SendPlayerIdWithConnectionRequest();
                NetworkManager.Singleton.StartClient();
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("PlayerStatusMessage",
                    OnPlayerStatusMessage);
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("HostLeaving", OnHostLeavingMessageReceived);
                _client.IsConnectedToServer = true;
                return true;
            }
            catch (OperationCanceledException) {
           
            }
            _client.IsConnectedToServer = false;
            return false;
        }
        
        /// <summary>
        /// Request a server allocation and return the connection information required to join the server.
        /// While the server allocation request is in process, the status of the machine hosting
        /// the server is stored and the UI updated.
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <param name="webServicesAPI">WebServicesAPI</param>
        /// <returns>The server's IP and Port inside a ClientConnectionInfo object</returns>
        private async Task<ClientConnectionInfo> GetClientConnectionInfo(CancellationToken cancellationToken, WebServicesAPI webServicesAPI) {
            
           // Queue allocation request for a game server
           await webServicesAPI.QueueAllocationRequest();
           
           // Check for an active machine
           var status  = await webServicesAPI.GetMachineStatus();
           UpdateServerStatusForLobbyHost(status);
           await _gameLobbyManager.UpdateLobbyWithServerInfo(status, "", "");
           
           Debug.Log("Initial Machine Status: " + status);
           
           // If no machine is found yet, keep polling until one is
           if (status != MachineStatus.Online) {
               var machines = await webServicesAPI.PollForMachine(60 * 5, cancellationToken);
               
               if (machines.Length == 0) {
                   Debug.LogError("No machines found after polling.");
                   return new ClientConnectionInfo { IP = "", Port = 0 };
               }
               
               var maxRetries = 60;
               var retryCount = 0;
               
               // Once a machine is found, poll for its status until it's online
               while (retryCount < maxRetries) {
                   Debug.Log("Polling Machine Status");
                   var newStatus = await webServicesAPI.GetMachineStatus();
                   if (status != newStatus) {
                       Debug.Log("Machine Status Change: " + newStatus);
                       status = newStatus;
                       await _gameLobbyManager.UpdateLobbyWithServerInfo(status, "", "");
                       UpdateServerStatusForLobbyHost(status);
                       if (status == MachineStatus.Online) {
                           break;
                       }
                   }
                   retryCount++;
                   await Task.Delay(5000, cancellationToken);
               }
           }
           else {
               await _gameLobbyManager.UpdateLobbyWithServerInfo(status, "", "");
               UpdateServerStatusForLobbyHost(status);
           }
           
           // Now the machine is online, poll for the IP and Port of allocated game server
           var response = await webServicesAPI.PollForAllocation(60 * 5, cancellationToken);
           return response != null
               ? new ClientConnectionInfo { IP = response.ipv4, Port = response.gamePort }
               : new ClientConnectionInfo { IP = "", Port = 0 }; 
        }

        /// <summary>
        /// Update the server status in the Lobby View for the lobby host
        /// </summary>
        /// <param name="status">Server Status</param>
        private void UpdateServerStatusForLobbyHost(string status) {
            _client.ServerStatus = status;
            if (status == "") {
                _client.ServerStatus = "Inactive";
            }
            //_viewManager.RePaintView(typeof(LobbyView));
        }

        /// <summary>
        /// Update the server info in the Lobby View for the lobby host
        /// </summary>
        /// <param name="ip">Server IP</param>
        /// <param name="port">Server Port</param>
        private void UpdateServerInfoForLobbyHost(string ip, string port) {
            _client.ServerIP = ip;
            _client.Port = port;
            //_viewManager.RePaintView(typeof(LobbyView));
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
                //_viewManager.RePaintView(typeof(LobbyView));
            }
        }

        /// <summary>
        /// Process server initiated disconnect because lobby host has disconnected 
        /// </summary>
        private async void OnHostLeavingMessageReceived(ulong clientId, FastBufferReader reader) {
            if (!_client.IsLobbyHost) {
                await Disconnect();
                //_viewManager.RePaintView(typeof(LobbyView));
                //_viewManager.ChangeView(typeof(LobbyView));
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
        private async void OnClientDisconnected(ulong clientId) {
            Debug.Log("Client disconnected (from server): " + clientId);
        }
    }
}