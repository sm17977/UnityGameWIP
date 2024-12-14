using Unity.Netcode;
using UnityEngine;

public struct StatePayload : INetworkSerializable {
    public int Tick;
    public Vector3 Position; 
    public Vector3 TargetPosition; 
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref Tick);
        serializer.SerializeValue(ref Position);
        serializer.SerializeValue(ref TargetPosition);
    }
}
