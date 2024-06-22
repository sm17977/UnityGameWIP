
using Unity.Netcode;
public enum InputCommandType { Movement, CastSpell, Attack, None, Init};
public struct InputCommand : INetworkSerializable{

    public InputCommandType type;
    public double time;
    public string key;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter{
        serializer.SerializeValue(ref type);
        serializer.SerializeValue(ref time);
        serializer.SerializeValue(ref key);
    }
}
