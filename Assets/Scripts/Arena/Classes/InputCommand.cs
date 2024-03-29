
public enum InputCommandType { Movement, CastSpell, Attack, None};
public class InputCommand {

    public InputCommandType type;
    public double time;

    public Ability ability;


}
