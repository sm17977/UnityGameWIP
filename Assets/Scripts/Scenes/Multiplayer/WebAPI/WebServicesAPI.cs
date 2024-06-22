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
        private const int BuildConfigurationId = 1266240;
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
            _authHeader = GenerateAuthHeader();
        }

        private string GenerateAuthHeader() {
            var keyByteArray = Encoding.UTF8.GetBytes($"{KeyId}:{SecretKey}");
            return Convert.ToBase64String(keyByteArray);
        }

        private async Task<UnityWebRequest> SendRequest(string uri, string method, string payload = null,
            string token = null) {
            UnityWebRequest www;
            if (method == "POST") {
                www = new UnityWebRequest(uri, "POST");
                if (!string.IsNullOrEmpty(payload)) {
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
                    www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    www.SetRequestHeader("Content-Type", "application/json");
                }
            }
            else {
                www = UnityWebRequest.Get(uri);
            }

            www.downloadHandler = new DownloadHandlerBuffer();
            if (!string.IsNullOrEmpty(token)) {
                www.SetRequestHeader("Authorization", token);
            }
            else {
                www.SetRequestHeader("Authorization", "Basic " + _authHeader);
            }

            await www.SendWebRequest();

            return www;
        }

        private async Task<T> HandleResponse<T>(UnityWebRequest www) {
            if (www.result == UnityWebRequest.Result.ConnectionError ||
                www.result == UnityWebRequest.Result.ProtocolError) {
                Debug.LogError($"Error: {www.error}");
                return default;
            }

            var jsonResponse = www.downloadHandler.text;
            try {
                return JsonConvert.DeserializeObject<T>(jsonResponse);
            }
            catch (Exception e) {
                Debug.LogError($"Error: {e}");
                return default;
            }
        }

        public async Task RequestAPIToken() {
            var payload = new {
                scopes = new[] {
                    "multiplay.allocations.create", "multiplay.allocations.list", "multiplay.allocations.get",
                    "multiplay.machines.list"
                }
            };
            var jsonPayload = JsonConvert.SerializeObject(payload);
            var uri = Protocol + ServicesDomain + TokenExchangeEndpoint;

            using var www = await SendRequest(uri, "POST", jsonPayload);
            var response = await HandleResponse<TokenExchangeResponse>(www);
            _apiToken = response?.accessToken;
            Debug.Log($"Access Token: {_apiToken}");
        }

        public async Task QueueAllocationRequest() {
            var payload = new QueueAllocationRequest {
                buildConfigurationId = BuildConfigurationId,
                allocationId = AllocationId,
                regionId = RegionId
            };
            var jsonPayload = JsonConvert.SerializeObject(payload);
            var uri = Protocol + MultiplayDomain + QueueAllocationRequestEndpoint;

            using var www = await SendRequest(uri, "POST", jsonPayload, "Bearer " + _apiToken);
            var response = await HandleResponse<QueueAllocationResponse>(www);
            Debug.Log($"Allocation ID: {response?.allocationId}");
            Debug.Log($"href: {response?.href}");
        }

        public async Task<GetAllocationResponse> GetAllocationRequest() {
            var uri = Protocol + MultiplayDomain + GetAllocationEndpoint;
            using var www = await SendRequest(uri, "GET", token: "Bearer " + _apiToken);
            return await HandleResponse<GetAllocationResponse>(www);
        }

        public async Task<GetAllocationResponse> PollForAllocation(int timeoutSeconds,
            CancellationToken cancellationToken) {
            var elapsed = 0;
            const int pollInterval = 5;
            GetAllocationResponse response;

            do {
                Debug.Log("Polling for allocation");
                cancellationToken.ThrowIfCancellationRequested();
                response = await GetAllocationRequest();
                if (!string.IsNullOrEmpty(response?.ipv4)) return response;
                await Task.Delay(pollInterval * 1000, cancellationToken);
                elapsed += pollInterval;
            } while (elapsed < timeoutSeconds);

            Debug.LogError("Polling timed out.");
            return null;
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
    }
}