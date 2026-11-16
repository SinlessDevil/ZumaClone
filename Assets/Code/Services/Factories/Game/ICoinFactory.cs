using Code.Logic.Zuma.Level;
using UnityEngine;

namespace Code.Services.Factories.Game
{
    public interface ICoinFactory
    {
        ZumaCoin CreateCoin(Transform parent);
    }
}
