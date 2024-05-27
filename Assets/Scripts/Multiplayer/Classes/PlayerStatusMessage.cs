using Unity.Netcode;

namespace Multiplayer {
    public struct PlayerStatusMessage : INetworkSerializable
    {
        public ulong ClientId;
        public bool IsConnected;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref IsConnected);
        }
    }
}