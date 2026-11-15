using System;
using Code.Logic.Zuma.Balls;
using Code.StaticData.Levels;
using UnityEngine;

namespace Code.StaticData
{
    [CreateAssetMenu(fileName = "BallConfig", menuName = "StaticData/Ball")]
    public sealed class BallStaticData : ScriptableObject
    {
        public Material Material;
        public BallTextureSet DefaultTextures;
        public BallColorAppearance[] Colors = Array.Empty<BallColorAppearance>();
        public BallPowerupAppearance[] Powerups = Array.Empty<BallPowerupAppearance>();

        public BallTextureSet GetTextures(Color color, BallPowerup powerup)
        {
            if (powerup != BallPowerup.None)
            {
                foreach (BallPowerupAppearance appearance in Powerups)
                    if (appearance.Powerup == powerup)
                        return appearance.Textures;
            }

            foreach (BallColorAppearance appearance in Colors)
                if (appearance.Item.ItemColor.Equals(color))
                    return appearance.Textures;

            return DefaultTextures;
        }
    }

    [Serializable]
    public sealed class BallTextureSet
    {
        public Texture2D BaseColor;
        public Texture2D Normal;
        public Texture2D RuneMask;
    }

    [Serializable]
    public sealed class BallColorAppearance
    {
        public ItemConfig Item;
        public BallTextureSet Textures = new();
    }

    [Serializable]
    public sealed class BallPowerupAppearance
    {
        public BallPowerup Powerup;
        public BallTextureSet Textures = new();
    }
}
