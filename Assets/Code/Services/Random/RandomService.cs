using System;
using System.Collections.Generic;
using Code.StaticData.Levels;
using System.Linq;
using Code.Logic.Zuma;
using Code.Services.Levels;
using UnityEngine;

namespace Code.Services.Random
{
    public class RandomService : IRandomService
    {
        private readonly ILevelService _levelService;
        private readonly System.Random _ammoRandom = new();

        public RandomService(ILevelService levelService)
        {
            _levelService = levelService;
        }

        public List<Color> GetColorsByLevelRandomConfig()
        {
            LevelConfig config = _levelService.GetCurrentLevelStaticData().LevelConfig;
            var palette = BuildPalette(config.RandomConfig);
            if (palette.Count == 0 || config.CountItem < 1)
                throw new InvalidOperationException("A level needs a positive opening count and at least one weighted ball color.");

            System.Random opening = new(UnityEngine.Random.Range(0, int.MaxValue));
            List<Color> colors = new(config.CountItem);
            Color? previous = null;
            for (int i = 0; i < config.CountItem; i++)
            {
                Color color = Sample(palette, opening, previous, config.RandomConfig.RepeatColorPercent);
                colors.Add(color);
                previous = color;
            }

            return colors;
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

        public static List<(Color color, float weight)> BuildPalette(LevelRandomConfigStaticData config)
        {
            if (config == null)
                return new List<(Color, float)>();

            return config.ItemProbabilities
                .Where(entry => entry.Item != null && entry.Item.ItemColor.a > 0f && entry.Probability > 0f)
                .GroupBy(entry => entry.Item.ItemColor)
                .Select(group => (group.Key, group.Sum(entry => entry.Probability)))
                .ToList();
        }

        public static Color Sample(IReadOnlyList<(Color color, float weight)> palette, System.Random random,
            Color? previous, float repeatPercent)
        {
            double totalWeight = 0d;
            bool includesPrevious = false;
            Color fallbackColor = palette[0].color;
            foreach ((Color color, float weight) in palette)
            {
                if (previous.HasValue && color == previous.Value)
                {
                    includesPrevious = true;
                    continue;
                }

                totalWeight += weight;
                fallbackColor = color;
            }

            if (includesPrevious && (totalWeight == 0d || random.NextDouble() * 100d < repeatPercent))
                return previous.Value;

            double roll = random.NextDouble() * totalWeight;
            foreach ((Color color, float weight) in palette)
            {
                if (previous.HasValue && color == previous.Value)
                    continue;

                roll -= weight;
                if (roll < 0d)
                    return color;
            }

            return fallbackColor;
        }

        private Color SelectColorFromAvailable(IEnumerable<Color> colors, Color? excludeColor)
        {
            var availableColors = colors?
                .Where(color => color.a > 0f)
                .Distinct()
                .ToList() ?? new List<Color>();

            if (availableColors.Count == 0)
                return Color.clear;

            if (excludeColor.HasValue && availableColors.Count > 1)
                availableColors.Remove(excludeColor.Value);

            return availableColors[_ammoRandom.Next(availableColors.Count)];
        }

        public string GenerateId() => System.Guid.NewGuid().ToString();
    }
}
