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
            {
                return new List<Color>();
            }

            List<Color> selectedColors = new List<Color>();
            
            foreach (var itemProbability in levelRandomConfig.ItemProbabilities)
            {
                int exactCount = Mathf.RoundToInt(count * (itemProbability.Probability / 100f));
                for (int i = 0; i < exactCount; i++)
                {
                    selectedColors.Add(itemProbability.Item.ItemColor);
                }
            }
            
            for (int i = selectedColors.Count - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1);
                (selectedColors[i], selectedColors[randomIndex]) = (selectedColors[randomIndex], selectedColors[i]);
            }

            return selectedColors;
        }

        public Color GetColorByCurrentItems(List<Item> items, Item currentItem)
        {
            if (items == null || items.Count == 0)
                return Color.clear;
    
            List<Color> availableColors = items
                .Select(item => item.Color)
                .Distinct()
                .ToList();
    
            if (currentItem != null && availableColors.Contains(currentItem.Color) && availableColors.Count > 1)
                availableColors.Remove(currentItem.Color);
    
            Color result = availableColors[UnityEngine.Random.Range(0, availableColors.Count)];
            return result;
        }
        
        public Color GetColorByCurrentItems(List<Color> colors, Color? currentColor)
        {
            if (colors == null || colors.Count == 0)
                return Color.clear;

            List<Color> availableColors = colors
                .Distinct()
                .ToList();

            if (currentColor.HasValue && availableColors.Contains(currentColor.Value) && availableColors.Count > 1)
            {
                availableColors.Remove(currentColor.Value);
            }

            Color result = availableColors[UnityEngine.Random.Range(0, availableColors.Count)];
            return result;
        }
        
        public string GenerateId() => System.Guid.NewGuid().ToString();
    }
}