using Unity.Netcode;
using UnityEngine;

public struct InputPayload : INetworkSerializable {
    public int Tick;
    public Vector3 TargetPosition;
    public string AbilityKey;
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref Tick);
        serializer.SerializeValue(ref TargetPosition);
        serializer.SerializeValue(ref AbilityKey);
    }
}
