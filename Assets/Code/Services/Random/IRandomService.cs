using System.Collections.Generic;
using Code.Logic.Zuma;
using UnityEngine;

namespace Code.Services.Random
{
    public interface IRandomService
    {
        string GenerateId();
        List<Color> GetColorsByLevelRandomConfig();
        Color GetColorByCurrentItems(List<Item> items, Item currentItem);
    }
}