using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;

namespace Multiplayer
{
    public class WebServicesAPI {
        
        private const string KeyId = "75be35dd-aab9-427b-aa3f-944663996fef";
        private const string SecretKey = "wXwj3X_NOQR2F0QGmu0jwKn-SSN5W0NA";
        private const string EnvId = "5a3b71b7-e73a-4cdd-8c5b-d855a2787d9e";
        private const string ProjectId = "365c4d7c-99f6-4eda-82f9-c2940d90904c";
        
        private const string FleetId = "89e28258-1a94-480a-9fe8-f547da5a516e";
        private const int BuildConfigurationId = 1263149;
        private const string AllocationId = "9ec871ce-3363-4de2-8f79-062881067628"; // TODO - Generate this
        private const string RegionId = "34ca86e6-7050-4276-b889-9ba3d11db960";
        
        private static string _authHeader;
        private static string _apiToken;

        private const string Protocol = "https://";
        private const string ServicesDomain = "services.api.unity.com";
        private const string MultiplayDomain = "multiplay.services.api.unity.com";

        private static readonly string ListServersEndpoint =
            $"/multiplay/servers/v1/projects/{ProjectId}/environments/{EnvId}/servers";
        
        private static readonly string ListMachinesEndpoint =
            $"/multiplay/machines/v1/projects/{ProjectId}/environments/{EnvId}/machines";

        private static readonly string TokenExchangeEndpoint =
            $"/auth/v1/token-exchange?projectId={ProjectId}&environmentId={EnvId}";

        private static readonly string QueueAllocationRequestEndpoint =
            $"/v1/allocations/projects/{ProjectId}/environments/{EnvId}/fleets/{FleetId}/allocations";

        private static readonly string GetAllocationEndpoint = 
            $"/v1/allocations/projects/{ProjectId}/environments/{EnvId}/fleets/{FleetId}/allocations/{AllocationId}";
        
        public WebServicesAPI() {
            _authHeader = GetAuthHeader();
        }

        // Generate services auth header
        private string GetAuthHeader() {
            var keyBteArray = Encoding.UTF8.GetBytes(KeyId + ":" + SecretKey);
            return Convert.ToBase64String(keyBteArray);
        }
        
        // Request the stateless auth token required for certain requests
        public async Task RequestAPIToken() {

            var payload = new {
                scopes = new[] { "multiplay.allocations.create", "multiplay.allocations.list" ,
                    "multiplay.allocations.get", "multiplay.machines.list"} 
            };

            string jsonPayload = JsonConvert.SerializeObject(payload);
            
            Debug.Log($"Payload: {jsonPayload}");
            var uri = Protocol + ServicesDomain + TokenExchangeEndpoint;
            using var www = UnityWebRequest.Post(uri, jsonPayload, "application/json");
            www.SetRequestHeader("Authorization", "Basic " + _authHeader);

            var operation = www.SendWebRequest();
            
            while (!operation.isDone) {
                await Task.Yield();
            }

            if (www.result == UnityWebRequest.Result.ConnectionError || 
                www.result == UnityWebRequest.Result.ProtocolError) {
                Debug.LogError($"Error: {www.error}");
            }
            else {
                var jsonResponse = www.downloadHandler.text;
                try {
                    TokenExchangeResponse response = JsonConvert.DeserializeObject<TokenExchangeResponse>(jsonResponse);
                    _apiToken = response.accessToken;
                    Debug.Log($"Access Token: {response.accessToken}");
                }
                catch (Exception e) {
                    Debug.LogError($"Error: {e}");
                }
            }
        }

        // Request a new server
        public async Task QueueAllocationRequest() {
            
             var payload = new QueueAllocationRequest {
                 buildConfigurationId = BuildConfigurationId,
                 allocationId = AllocationId,
                 regionId = RegionId
             };

            string jsonPayload = JsonConvert.SerializeObject(payload);
            
            var uri = Protocol + MultiplayDomain + QueueAllocationRequestEndpoint;
          
            using var www = UnityWebRequest.Post(uri, jsonPayload, "application/json");
            www.SetRequestHeader("Authorization", "Bearer " + _apiToken);
           

            var operation = www.SendWebRequest();
            
            while (!operation.isDone) {
                await Task.Yield();
            }

            if (www.result == UnityWebRequest.Result.ConnectionError || 
                www.result == UnityWebRequest.Result.ProtocolError) {
                Debug.LogError($"Error: {www.error}");
            }
            else {
                var jsonResponse = www.downloadHandler.text;
                try {
                    QueueAllocationResponse response = JsonConvert.DeserializeObject<QueueAllocationResponse>(jsonResponse);
                    Debug.Log($"Allocation ID: {response.allocationId}");
                    Debug.Log($"href: {response.href}");
                }
                catch (Exception e) {
                    Debug.LogError($"Error: {e}");
                }
            }
        }

        // Get an allocation request 
        public async Task<GetAllocationResponse> GetAllocationRequest() {

            GetAllocationResponse response = new GetAllocationResponse();
            
            var uri = Protocol + MultiplayDomain + GetAllocationEndpoint;
            using var www = UnityWebRequest.Get(uri);
            www.SetRequestHeader("Authorization", "Bearer " + _apiToken);
            Debug.Log($"Get Allocation Request Token: {_apiToken}");

            var operation = www.SendWebRequest();
            
            while (!operation.isDone) {
                await Task.Yield();
            }

            if (www.result == UnityWebRequest.Result.ConnectionError || 
                www.result == UnityWebRequest.Result.ProtocolError) {
                Debug.LogError($"Error: {www.error}");
            }
            else {
                var jsonResponse = www.downloadHandler.text;
                try {
                    response = JsonConvert.DeserializeObject<GetAllocationResponse>(jsonResponse);
                    Debug.Log($"Allocation ID: {response.allocationId}");
                    Debug.Log($"Readiness: {response.readiness}");
                    Debug.Log($"Created: {response.created}");
                    Debug.Log($"Game Port: {response.gamePort}");
                    Debug.Log($"IPv4: {response.ipv4}");
             
                    return response;
                }
                catch (Exception e) {
                    Debug.LogError($"Error: {e}");
                }
            }
            return response;
        }
        
        // Poll the allocation request, because the response will not contain the server IP if it's not ready yet
        public async Task<GetAllocationResponse> PollForAllocation(int timeoutSeconds, CancellationToken cancellationToken) {
            int elapsed = 0;
            const int pollInterval = 5;
            GetAllocationResponse response = await GetAllocationRequest();

            while (string.IsNullOrEmpty(response.ipv4) && elapsed < timeoutSeconds) {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Delay(pollInterval * 1000, cancellationToken);
                elapsed += pollInterval;
                response = await GetAllocationRequest();
            }

            if (string.IsNullOrEmpty(response.ipv4)) {
                Debug.LogError("Polling timed out.");
                return null;
            }
            return response;
        }

        // Get the current list of servers
        public async Task<Server[]> GetServerList() {
            Server[] servers = Array.Empty<Server>();
            
            var uri = Protocol + ServicesDomain + ListServersEndpoint;
            using var www = UnityWebRequest.Get(uri);
            www.SetRequestHeader("Authorization", "Basic " + _authHeader);

            var operation = www.SendWebRequest();
            
            while (!operation.isDone) {
                await Task.Yield();
            }

            if (www.result == UnityWebRequest.Result.ConnectionError || 
                www.result == UnityWebRequest.Result.ProtocolError) {
                Debug.LogError($"Error: {www.error}");
            }
            else {
                var jsonResponse = www.downloadHandler.text;
                try {
                    Debug.Log($"Success: {jsonResponse}");
                    servers = JsonConvert.DeserializeObject<Server[]>(jsonResponse);
                    foreach (Server server in servers) {
                        Debug.Log($"Server: {server.buildConfigurationName}, IP: {server.ip}, Port: {server.port}");
                    }
                    return servers;
                }
                catch (Exception e) {
                    Debug.LogError($"Error: {e}");
                    Server[] emptyServersArray;
                }
            }
            return servers;
        }

        public async Task<Machine[]> GetMachineList() {
            Machine[] machines = Array.Empty<Machine>();
            
            var uri = Protocol + ServicesDomain + ListMachinesEndpoint;
            using var www = UnityWebRequest.Get(uri);
            www.SetRequestHeader("Authorization", "Basic " + _authHeader);

            var operation = www.SendWebRequest();
            
            while (!operation.isDone) {
                await Task.Yield();
            }

            if (www.result == UnityWebRequest.Result.ConnectionError || 
                www.result == UnityWebRequest.Result.ProtocolError) {
                Debug.LogError($"Error: {www.error}");
            }
            else {
                var jsonResponse = www.downloadHandler.text;
                try {
                    Debug.Log($"Success: {jsonResponse}");
                    machines = JsonConvert.DeserializeObject<Machine[]>(jsonResponse);
                    foreach (Machine machine in machines) {
                        Debug.Log($"Machine: {machine.ip}, Status: {machine.status}");
                    }
                    return machines;
                }
                catch (Exception e) {
                    Debug.LogError($"Error: {e}");
                    Server[] emptyServersArray;
                }
            }
            return machines;
        }

        public async Task<string> GetMachineStatus(string serverIp) {

            Machine[] machines = await GetMachineList();
            
            foreach (Machine machine in machines) {
                if (machine.ip == serverIp) {
                    return machine.status;
                }
            }

            return "";
        }
    }
}
