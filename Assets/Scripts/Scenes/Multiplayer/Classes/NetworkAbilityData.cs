using Unity.Netcode;

public class NetworkAbilityData : INetworkSerializable {
    public float currentCooldown;
    public float maxCooldown;
    public string key;

    // Default constructor for deserialization
    public NetworkAbilityData() {}

    // Constructor to initialize the data from an Ability ScriptableObject
    public NetworkAbilityData(Ability ability) {
        currentCooldown = ability.currentCooldown;
        maxCooldown = ability.maxCooldown;
        key = ability.key;
    }

    // Implement the Write and Read methods for custom serialization
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        // Serialize each field
        serializer.SerializeValue(ref currentCooldown);
        serializer.SerializeValue(ref maxCooldown);
        serializer.SerializeValue(ref key);
    }
}