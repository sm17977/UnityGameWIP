using System;
using System.Collections.Generic;

public class BuffEffectFactory {
    private static readonly Dictionary<string, Func<BuffEffect>> EffectRegistry = new Dictionary<string, Func<BuffEffect>> {
        { "MoveSpeedEffect", () => new MoveSpeedEffect() }

    };

    public static BuffEffect CreateEffect(string effectType) {
        if (EffectRegistry.TryGetValue(effectType, out var creator)) {
            return creator();
        }

        throw new Exception($"BuffEffect type '{effectType}' not recognized.");
    }
}