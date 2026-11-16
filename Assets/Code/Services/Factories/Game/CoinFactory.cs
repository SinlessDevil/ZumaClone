using Code.Logic.Zuma.Level;
using UnityEngine;
using Zenject;

namespace Code.Services.Factories.Game
{
    public sealed class CoinFactory : Factory, ICoinFactory
    {
        public CoinFactory(IInstantiator instantiator) : base(instantiator)
        {
        }

        public ZumaCoin CreateCoin(Transform parent)
        {
            GameObject instance = Instantiate(ResourcePath.CoinPath);
            instance.transform.SetParent(parent, false);
            ZumaCoin coin = instance.GetComponent<ZumaCoin>();
            coin.Hide();
            return coin;
        }
    }
}
