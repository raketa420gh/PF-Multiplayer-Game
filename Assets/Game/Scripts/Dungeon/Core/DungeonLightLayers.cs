using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game.Scripts.Dungeon
{
    /// Rendering layers of the catacomb light. Lightmapped stone sits on its own layer, so a baked lamp, kept alive as a real-time
    /// light for bodies, props and the walls between rooms, does not light that stone twice. Every other light reaches both layers.
    public static class DungeonLightLayers
    {
        public const uint Default = 1u;
        public const uint BakedStone = 2u;
        public const uint Everything = Default | BakedStone;

        public static void Set(Light light, uint layers)
        {
            light.GetUniversalAdditionalLightData().renderingLayers = layers;
        }
    }
}
