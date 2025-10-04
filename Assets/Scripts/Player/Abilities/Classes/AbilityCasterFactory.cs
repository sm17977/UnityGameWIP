public static class AbilityCasterFactory {
    public static AbilityCaster Get(Ability ability, LuxPlayerController playerController, bool isMultiplayer) {
        ICastBehaviour behaviour = ability.archetype switch {
            Ability.AbilityArchetype.Projectile => new ProjectileCastBehaviour(),
            Ability.AbilityArchetype.Aoe => new AoeCastBehaviour(),
            _ => throw new System.Exception("You didn't set an ability archetype")
        };

        IExecutionStrategy execution = isMultiplayer
            ? new NetcodeExecution()
            : new SinglePlayerExecution();
        
        return new AbilityCaster(behaviour, execution, playerController);
    }
}
