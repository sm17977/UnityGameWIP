public class MoveSpeedEffect : BuffEffect {
    public override void ApplyEffect(LuxController target, float strength) {
        target.canMove = false;
    }
    public override void RemoveEffect(LuxController target, float strength) {
        target.canMove = true;
    }
}