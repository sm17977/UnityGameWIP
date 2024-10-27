
using Unity.Netcode;
public enum InputCommandType { Movement, CastSpell, Attack, None, Init};
public struct InputCommand{
    public InputCommandType Type;
    public string Key;
    public Ability Ability;
}
