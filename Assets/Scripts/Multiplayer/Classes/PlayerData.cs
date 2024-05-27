namespace Multiplayer {
    using Unity.Netcode;

    public struct PlayerData : INetworkSerializable {
        public ulong ClientId;
        public string PlayerName; 
        public bool IsConnected;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref IsConnected);
        }
    }
}