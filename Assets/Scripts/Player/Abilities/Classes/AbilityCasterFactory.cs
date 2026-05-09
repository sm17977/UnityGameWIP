public static class AbilityCasterFactory {
    public static AbilityCaster Get(LuxPlayerController playerController, bool isMultiplayer) {
        return new AbilityCaster(playerController, isMultiplayer);
    }
}
