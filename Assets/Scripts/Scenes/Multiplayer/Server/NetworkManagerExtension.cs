using Unity.Collections;
using Unity.Netcode;

namespace Multiplayer {
    public static class NetworkManagerExtensions {
        public static void SendCustomMessageToClient<T>(this NetworkManager networkManager, ulong clientId, T message) where T : INetworkSerializable, new() {
            using (var writer = new FastBufferWriter(128, Allocator.Temp)) {
                writer.WriteNetworkSerializable(message);
                networkManager.CustomMessagingManager.SendNamedMessage("PlayerStatusMessage", clientId, writer);
            }
        }
        
        public static void SendHostLeavingMessageToServer<T>(this NetworkManager networkManager, ulong clientId, T message) where T : INetworkSerializable, new() {
            using (var writer = new FastBufferWriter(128, Allocator.Temp)) {
                writer.WriteNetworkSerializable(message);
                networkManager.CustomMessagingManager.SendNamedMessage("HostLeaving", clientId, writer);
            }
        }
        
        
        public static void SendHostLeavingMessageToClient<T>(this NetworkManager networkManager, ulong clientId, T message) where T : INetworkSerializable, new() {
            using (var writer = new FastBufferWriter(128, Allocator.Temp)) {
                writer.WriteNetworkSerializable(message);
                networkManager.CustomMessagingManager.SendNamedMessage("HostLeaving", clientId, writer);
            }
        }
    }
}