using System.Threading.Tasks;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System;
using System.Text;
using UnityEngine;

namespace Scenes.Multiplayer.EdgeGapAPI {
    public class EdgeGapClient {

        private const string Auth = "d1a1ae5f-df47-403d-bb11-23dc5e59c24c";
        private const string ApiPath = "api.edgegap.com";
        private const string Scheme = "https";
        private const string AppName = "leaguetoolv2";
        private const string Version = "v0.0.1";
        

        public EdgeGapClient() {
            
        }   
        
        private async Task<UnityWebRequest> SendRequest(string endpoint, string method, string version, string payload = null) {
            
            var uri = $"{Scheme}://{ApiPath}/{version}/{endpoint}";
            
            var request = new UnityWebRequest(uri, method);
            request.downloadHandler = new DownloadHandlerBuffer();

            if (!string.IsNullOrEmpty(payload)) {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.SetRequestHeader("Content-Type", "application/json");
            }

            request.SetRequestHeader("Authorization", $"token {Auth}");
            request.SetRequestHeader("Accept", "application/json");
            
            await request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success) {
                Logger.LogResponse(method, uri, request.responseCode, request.downloadHandler.text, 1);
            }
            else {
                Logger.LogError(method, uri, request.responseCode,
                    $"{request.error}\nResponse: {request.downloadHandler?.text}");
            }
            return request;
        }

        /// <summary>
        /// Deploys a new server
        /// </summary>
        /// <returns>Deployment request ID</returns>
        public async Task<string> Deploy(User[] users) {

            var body = JsonConvert.SerializeObject(new {
                application = AppName,
                version = Version,
                users = users,
            });
            
            var response =  await SendRequest("deployments","POST", "v2", body);
            var deserialized = JsonConvert.DeserializeObject<DeployResponse>(response.downloadHandler.text);
            return deserialized.request_id;
        }
        
        public async Task<GetDeploymentResponse> GetDeploymentStatus(string requestId) {
            var response = await SendRequest($"status/{requestId}", "GET", "v1");
            return JsonConvert.DeserializeObject<GetDeploymentResponse>(response.downloadHandler.text);
        }

        public async Task<string> StopDeployment(string requestId) {
            var response = await SendRequest($"stop/{requestId}", "DELETE", "v1");
            return response.downloadHandler.text;
        }
        
        public async Task<string> GetIPAddress() {
            using var request = UnityWebRequest.Get("https://api.ipify.org");
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success) {
                Debug.LogError(request.error);
                return null;
            }

            return request.downloadHandler.text;
        }

        // public async Task<> ListAllDeployments() {
        //     var response = await SendRequest("deployments", "GET", "v1");
        // }
        
        
        
        
    


    }
}