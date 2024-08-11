using System;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using Multiplayer.UI;
using QFSW.QC;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace Multiplayer {
    public delegate void OnUpdateServerDataInLobbyView(Client client); 
    public delegate void OnRePaintLobbyView(); 
    public delegate void OnHostDisconnection(); 
    public class ClientManager : MonoBehaviour {

        public event OnUpdateServerDataInLobbyView UpdateServerDataInLobbyView;
        public event OnRePaintLobbyView RePaintLobbyView;
        public event OnHostDisconnection HostDisconnection;

        public static ClientManager Instance;
        public Client Client;
        private CancellationTokenSource _cancellationTokenSource;
        
        private GameLobbyManager _gameLobbyManager;
        
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
            
            Client = Client.Instance;
            _gameLobbyManager = GameLobbyManager.Instance;
        }

        private void Start() {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        
        /// <summary>
        /// Allocate and provision a multiplay server, share the connection details with the lobby
        /// </summary>
        /// <returns>boolean</returns>
        public async Task<bool> StartServer() {
            _cancellationTokenSource = new CancellationTokenSource();
            if (Client.IsLobbyHost) {
                return await ProvisionServer(_cancellationTokenSource.Token);
            }

            return false;
        }

        /// <summary>
        /// Disconnect the client form the multiplay server
        /// Update the player's connection status
        /// </summary>
        public async Task Disconnect() {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("Disconnecting and deleting client");
            ResetClient();
            await _gameLobbyManager.UpdatePlayerDataWithConnectionStatus(Client.IsConnectedToServer);
        }
        
        /// <summary>
        /// Queues a server allocation request and waits till server connection info has been returned.
        /// Once returned, updates the lobby with the connection info required to join the server.
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>boolean</returns>
        private async Task<bool> ProvisionServer(CancellationToken cancellationToken) {

            try {
                WebServicesAPI webServicesAPI = new WebServicesAPI();
                await webServicesAPI.RequestAPIToken();
                var clientConnectionInfo = await GetClientConnectionInfo(cancellationToken, webServicesAPI);
                
                if (clientConnectionInfo.IP != null || clientConnectionInfo.IP != "") {
                    UpdateServerInfoForLobbyHost(clientConnectionInfo.IP, clientConnectionInfo.Port.ToString());
                    await _gameLobbyManager.UpdateLobbyWithServerInfo(Client.ServerStatus, clientConnectionInfo.IP, clientConnectionInfo.Port.ToString());
                    return true;
                }
            }
            catch (OperationCanceledException) {
           
            }
            return false;
        }
        
        /// <summary>
        /// Start client to join the game server
        /// Before connecting to the server, the client sends their playerId in the connection approval payload,
        /// the server will receive this before approving, this enables us to track who has connected both from
        /// the client and server.
        /// </summary>
        /// <returns>boolean</returns>
        public bool StartClient() {

            if (Client.ServerIP == null || Client.Port == null) return false;
       
            try {
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(Client.ServerIP, (ushort)int.Parse(Client.Port));
                SendPlayerIdWithConnectionRequest();
                NetworkManager.Singleton.StartClient();
                NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("PlayerStatusMessage",
                    OnPlayerStatusMessage);
                if (!Client.IsLobbyHost) {
                    NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("HostLeaving", OnHostLeavingMessageReceived);
                }
                Client.IsConnectedToServer = true;
                return true;
            }
            catch (OperationCanceledException) {
           
            }
            Client.IsConnectedToServer = false;
            return false;
        }
        
        /// <summary>
        /// Request a server allocation and return the connection information required to join the server.
        /// While the server allocation request is in process, the current status of the machine hosting the
        /// server is stored and the lobby UI is updated.
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
                   //Debug.Log("Polling Machine Status");
                   var newStatus = await webServicesAPI.GetMachineStatus();
                   Debug.Log("New Status: " + newStatus);
                   Debug.Log("Retry count: " + retryCount);
                   if (status != newStatus) {
                       Debug.Log("Machine Status Change: " + newStatus);
                       status = newStatus;
                       await _gameLobbyManager.UpdateLobbyWithServerInfo(status, "", "");
                       UpdateServerStatusForLobbyHost(status);
                       if (status == MachineStatus.Online) {
                           Debug.Log("BREAK!");
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
           
           UpdateServerStatusForLobbyHost(status);
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
            Client.ServerStatus = status;
            if (status == "") {
                Client.ServerStatus = "INACTIVE";
            }
            UpdateServerDataInLobbyView?.Invoke(Client);
        }

        /// <summary>
        /// Update the server info (IP and Port) in the Lobby View for the lobby host
        /// </summary>
        /// <param name="ip">Server IP</param>
        /// <param name="port">Server Port</param>
        private void UpdateServerInfoForLobbyHost(string ip, string port) {
            Client.ServerIP = ip;
            Client.Port = port;
            UpdateServerDataInLobbyView?.Invoke(Client);
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
        /// Delete the existing server allocation
        /// </summary>
        public void DeleteAllocation() {
            ResetClient();
        }

        /// <summary>
        /// Reset the client instance by removing the server connection info
        /// </summary>
        public void ResetClient() {
            Client.ClearConnectionData();
        }
    }
}