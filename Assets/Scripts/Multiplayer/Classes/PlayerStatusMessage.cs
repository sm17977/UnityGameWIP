using Unity.Netcode;

namespace Multiplayer {
    public struct PlayerStatusMessage : INetworkSerializable
    {
        public string PlayerId;
        public bool IsConnected;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref PlayerId);
            serializer.SerializeValue(ref IsConnected);
        }
    }
}