using Code.Logic.Zuma.Level;
using Code.StaticData.Levels;

namespace Code.Services.Levels
{
    public interface ILevelGeometryService
    {
        void Apply(LevelHolder level, LevelStaticData data, LevelThemeStaticData theme = null);
    }
}
