using System.Collections.Generic;
using System.Linq;
using Code.Logic.Zuma;
using Code.Services.Levels;
using UnityEngine;

namespace Code.Services.Random
{
    public class RandomService : IRandomService
    {
        private readonly ILevelService _levelService;

        public RandomService(ILevelService levelService)
        {
            _levelService = levelService;
        }

        public List<Color> GetColorsByLevelRandomConfig()
        {
            var levelRandomConfig = _levelService.GetCurrentLevelStaticData().LevelConfig.RandomConfig;
            var count = _levelService.GetCurrentLevelStaticData().LevelConfig.CountItem;

            if (levelRandomConfig == null || levelRandomConfig.ItemProbabilities.Count == 0)
                return new List<Color>();

            List<Color> selectedColors = new();

            foreach (var itemProbability in levelRandomConfig.ItemProbabilities)
            {
                int exactCount = Mathf.RoundToInt(count * (itemProbability.Probability / 100f));
                for (int i = 0; i < exactCount; i++)
                    selectedColors.Add(itemProbability.Item.ItemColor);
            }

            // Перемешиваем
            for (int i = selectedColors.Count - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1);
                (selectedColors[i], selectedColors[randomIndex]) = (selectedColors[randomIndex], selectedColors[i]);
            }

            return selectedColors;
        }

        public Color GetColorByCurrentItems(List<Item> items, Item currentItem)
        {
            var colorList = items?
                .Select(i => i.Color)
                .ToList() ?? new List<Color>();

            return SelectColorFromAvailable(colorList, currentItem?.Color);
        }

        public Color GetColorByCurrentItems(List<Color> colors, Color? currentColor)
        {
            return SelectColorFromAvailable(colors, currentColor);
        }

        private Color SelectColorFromAvailable(IEnumerable<Color> colors, Color? excludeColor)
        {
            var availableColors = colors?
                .Distinct()
                .ToList() ?? new List<Color>();

            if (availableColors.Count == 0)
                return Color.clear;

            if (excludeColor.HasValue && availableColors.Count > 1)
                availableColors.Remove(excludeColor.Value);

            return availableColors[UnityEngine.Random.Range(0, availableColors.Count)];
        }

        public string GenerateId() => System.Guid.NewGuid().ToString();
    }
}
