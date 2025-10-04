using UnityEngine;

public class Logger {
    private const string LogPrefix = "[WebAPI]";
    
    public static void LogRequest(string method, string uri, string payload = null) {
        var message = $"{LogPrefix} Request: {method} {uri}";
        if (!string.IsNullOrEmpty(payload)) {
            message += $"\nPayload: {payload}";
        }
        Debug.Log(message);
    }

    public static void LogResponse(string method, string uri, long responseCode, string responseData, float duration) {
        var message = $"{LogPrefix} Response: {method} {uri}\n" +
                      $"Status Code: {responseCode}\n" +
                      $"Duration: {duration:F2}s\n" +
                      $"Response: {responseData}";
        Debug.Log(message);
    }

    public static void LogError(string method, string uri, long responseCode, string error) {
        var message = $"{LogPrefix} Error: {method} {uri}\n" +
                      $"Status Code: {responseCode}\n" +
                      $"Error: {error}";
        Debug.LogError(message);
    }
}