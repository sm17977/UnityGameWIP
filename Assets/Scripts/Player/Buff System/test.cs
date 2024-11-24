using Unity.Netcode;
using UnityEngine;

public struct NetworkBuffData : INetworkSerializable {
    public string EffectType;
    public string Key;
    public float Duration;
    public float EffectStrength;
    public float CurrentTimer;
    public string ID;
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref EffectType);
        serializer.SerializeValue(ref Key);
        serializer.SerializeValue(ref Duration);
        serializer.SerializeValue(ref EffectStrength);
        serializer.SerializeValue(ref CurrentTimer);
    }
}