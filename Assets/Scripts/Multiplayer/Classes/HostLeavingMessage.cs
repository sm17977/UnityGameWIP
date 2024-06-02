using Unity.Netcode;

namespace Multiplayer {
    public struct HostLeavingMessage : INetworkSerializable
    {
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            
        }
    }
}