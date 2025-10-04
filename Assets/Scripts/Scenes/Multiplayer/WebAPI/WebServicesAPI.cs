using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using WebSocketSharp;

namespace Multiplayer {
    public class WebServicesAPI {
        private const string KeyId = "75be35dd-aab9-427b-aa3f-944663996fef";
        private const string SecretKey = "wXwj3X_NOQR2F0QGmu0jwKn-SSN5W0NA";
        private const string EnvId = "5a3b71b7-e73a-4cdd-8c5b-d855a2787d9e";
        private const string ProjectId = "365c4d7c-99f6-4eda-82f9-c2940d90904c";
        private const string FleetId = "89e28258-1a94-480a-9fe8-f547da5a516e";
        private const int BuildConfigurationId = 1268952;
        private const string AllocationId = "9ec871ce-3363-4de2-8f79-062881067628"; // TODO - Generate this
        private const string RegionId = "34ca86e6-7050-4276-b889-9ba3d11db960";

        private static string _authHeader;
        private static string _apiToken;

        private const string Protocol = "https://";
        private const string ServicesDomain = "services.api.unity.com";
        private const string MultiplayDomain = "multiplay.services.api.unity.com";

        private static readonly string ListServersEndpoint =
            $"/multiplay/servers/v1/projects/{ProjectId}/environments/{EnvId}/servers";
        
        private static readonly string TriggerServerActionEndpoint =
            $"/multiplay/servers/v1/projects/{ProjectId}/environments/{EnvId}/servers";

        private static readonly string ListMachinesEndpoint =
            $"/multiplay/machines/v1/projects/{ProjectId}/environments/{EnvId}/machines";

        private static readonly string TokenExchangeEndpoint =
            $"/auth/v1/token-exchange?projectId={ProjectId}&environmentId={EnvId}";

        private static readonly string QueueAllocationRequestEndpoint =
            $"/v1/allocations/projects/{ProjectId}/environments/{EnvId}/fleets/{FleetId}/allocations";

        private static readonly string GetAllocationEndpoint =
            $"/v1/allocations/projects/{ProjectId}/environments/{EnvId}/fleets/{FleetId}/allocations/{AllocationId}";

        private static readonly string RemoveAllocationEndpoint =
            $"/v1/allocations/projects/{ProjectId}/environments/{EnvId}/fleets/{FleetId}/allocations/{AllocationId}";
        
        public WebServicesAPI() {
            _authHeader = GenerateAuthHeader();
        }

        private string GenerateAuthHeader() {
            var keyByteArray = Encoding.UTF8.GetBytes($"{KeyId}:{SecretKey}");
            return Convert.ToBase64String(keyByteArray);
        }

        private async Task<UnityWebRequest> SendRequest(string uri, string method, string payload = null, bool tokenRequired = false) {
            if (tokenRequired && string.IsNullOrEmpty(_apiToken)) await RequestAPIToken();
            var www = new UnityWebRequest(uri, method);
            www.downloadHandler = new DownloadHandlerBuffer();
            
            // Log the request
            Logger.LogRequest(method, uri, payload);
            var startTime = Time.realtimeSinceStartup;

            try {
                // Set up headers
                www.SetRequestHeader("Authorization", tokenRequired ? $"Bearer {_apiToken}" : $"Basic {_authHeader}");
                www.SetRequestHeader("Content-Type", "application/json");

                if (!string.IsNullOrEmpty(payload)) {
                    var bodyRaw = Encoding.UTF8.GetBytes(payload);
                    www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                }

                await www.SendWebRequest();

                // Calculate request duration
                var duration = Time.realtimeSinceStartup - startTime;

                if (www.result == UnityWebRequest.Result.Success) {
                    Logger.LogResponse(method, uri, www.responseCode, www.downloadHandler.text, duration);
                }
                else {
                    Logger.LogError(method, uri, www.responseCode,
                        $"{www.error}\nResponse: {www.downloadHandler?.text}");
                }


                return www;
            }
            catch (Exception ex) {
                Logger.LogError(method, uri, www.responseCode,
                    $"{www.error}\nResponse: {www.downloadHandler?.text}");
                throw;
            }
        }

        private async Task<T> HandleResponse<T>(UnityWebRequest www) {

            if (www.result != UnityWebRequest.Result.Success) {
                var errorMessage = $"Request failed: {www.error}\n" +
                                   $"Status Code: {www.responseCode}\n" +
                                   $"Response: {www.downloadHandler?.text}";
                Logger.LogError(www.method, www.url, www.responseCode, errorMessage);
                return default;
            }
            
            var jsonResponse = www.downloadHandler.text;
            
            try {
                return JsonConvert.DeserializeObject<T>(jsonResponse);
            }
            catch (Exception e) {
                Logger.LogError(www.method, www.url, www.responseCode,
                    $"JSON Deserialization Error: {e.Message}\nRaw Response: {jsonResponse}");
                return default;
            }
        }

        private async Task RequestAPIToken() {
            var payload = new {
                scopes = new[] {
                    "multiplay.allocations.create", "multiplay.allocations.list", "multiplay.allocations.get",
                    "multiplay.machines.list", "multiplay.allocations.delete", "multiplay.servers.start_stop"
                }
            };
            var jsonPayload = JsonConvert.SerializeObject(payload);
            var uri = Protocol + ServicesDomain + TokenExchangeEndpoint;

            using var www = await SendRequest(uri, "POST", jsonPayload);
            var response = await HandleResponse<TokenExchangeResponse>(www);
            _apiToken = response?.accessToken;
            Debug.Log($"Access Token: {_apiToken}");
        }

        public async Task<bool> QueueAllocationRequest() {
            var payload = new QueueAllocationRequest {
                buildConfigurationId = BuildConfigurationId,
                allocationId = AllocationId,
                regionId = RegionId
            };
            var jsonPayload = JsonConvert.SerializeObject(payload);
            var uri = Protocol + MultiplayDomain + QueueAllocationRequestEndpoint;
            
            Logger.LogRequest("POST", "QueueAllocation", jsonPayload);

            using var www = await SendRequest(uri, "POST", jsonPayload, true);
            var response = await HandleResponse<QueueAllocationResponse>(www);
           
            Logger.LogResponse("POST", "QueueAllocation", www.responseCode,
                $"Allocation ID: {response?.allocationId}, href: {response?.href}", 0);
            
            return !string.IsNullOrEmpty(response?.allocationId);
        }

        public async Task<GetAllocationResponse> GetAllocationRequest() {
            var uri = Protocol + MultiplayDomain + GetAllocationEndpoint;
            using var www = await SendRequest(uri, "GET", tokenRequired: true);
            return await HandleResponse<GetAllocationResponse>(www);
        }

        public async Task<GetAllocationResponse> PollForAllocation(int timeoutSeconds,
            CancellationToken cancellationToken) {
            var elapsed = 0;
            const int pollInterval = 5;
            GetAllocationResponse response;
            var pollCount = 0;

            do {
                pollCount++;
                Logger.LogRequest("GET", $"PollForAllocation (Attempt {pollCount})");
                
                cancellationToken.ThrowIfCancellationRequested();
                response = await GetAllocationRequest();

                Debug.Log("Response: " + response?.ipv4);
                
                if (!string.IsNullOrEmpty(response?.ipv4)) return response;
                
                await Task.Delay(pollInterval * 1000, cancellationToken);
                elapsed += pollInterval;
                
            } while (elapsed < timeoutSeconds);
            
            return null;
        }
        
        public async Task RemoveAllocation() {
            var uri = Protocol + MultiplayDomain + RemoveAllocationEndpoint;
            using var www = await SendRequest(uri, "DELETE", tokenRequired: true);
            var response = await HandleResponse<RemoveAllocationResponse>(www);
        }

        public async Task TriggerServerAction(ServerAction action, Server server) {
            
            var payload = new {
                action = action.ToString()
            };
            var jsonPayload = JsonConvert.SerializeObject(payload);

            var uri = Protocol + ServicesDomain + TriggerServerActionEndpoint + "/" + server.id + "/actions";
            using var www = await SendRequest(uri, "POST", jsonPayload);
            var response = await HandleResponse<RemoveAllocationResponse>(www);
        }
        
        public async Task<Machine[]> PollForMachine(int timeoutSeconds,
            CancellationToken cancellationToken) {
            var elapsed = 0;
            const int pollInterval = 5;
            Machine[] machines;

            do {
                cancellationToken.ThrowIfCancellationRequested();
                machines = await GetMachineList();

                if (machines.Length > 0) {
                    return machines;
                }
                
                await Task.Delay(pollInterval * 1000, cancellationToken);
                elapsed += pollInterval;
            } while (elapsed < timeoutSeconds);

            Debug.LogError("Polling timed out.");
            return null;
        }
        
        private async Task<Machine[]> GetMachineList() {
            var uri = Protocol + ServicesDomain + ListMachinesEndpoint;
            using var www = await SendRequest(uri, "GET");
            return await HandleResponse<Machine[]>(www) ?? Array.Empty<Machine>();
        }

        public async Task<string> GetMachineStatus() {
            var machines = await GetMachineList();
            return machines.Length > 0 ? machines[0].status : string.Empty;
        }
        
        public async Task<Server[]> GetServerList() {
            var uri = Protocol + ServicesDomain + ListServersEndpoint;
            using var www = await SendRequest(uri, "GET");
            return await HandleResponse<Server[]>(www) ?? Array.Empty<Server>();
        }

        public async Task<Server> GetServer(string ip, int port) {
            var servers = await GetServerList();
            foreach (var server in servers) {
                if (server.ip == ip && server.port == port) {
                    return server;
                }
            }
            return null;
        }
    }
}