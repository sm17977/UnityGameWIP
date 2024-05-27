namespace Multiplayer {
    using Unity.Netcode;

    public struct PlayerData : INetworkSerializable {
        public string PlayerId;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
            serializer.SerializeValue(ref PlayerId);
        }
    }
}