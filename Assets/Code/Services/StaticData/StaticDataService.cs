using System;
using System.Collections.Generic;
using System.Linq;
using Code.Services.BallController;
using Code.Services.Factories;
using Code.StaticData;
using Code.StaticData.Levels;
using Code.Window;
using UnityEngine;

namespace Code.Services.StaticData
{
    public class StaticDataService : IStaticDataService
    {
        private GameStaticData _gameStaticData;
        private GameplayInputStaticData _gameplayInputConfig;
        private BalanceStaticData _balanceStaticData;
        private Dictionary<WindowTypeId, WindowConfig> _windowConfigs;
        private List<ChapterStaticData> _chapterStaticDatas = new();
        private BallChainStaticData _ballChainConfig;
        private BallStaticData _ballConfig;
        
        public GameStaticData GameConfig => _gameStaticData;
        public GameplayInputStaticData GameplayInputConfig => _gameplayInputConfig;
        public BalanceStaticData Balance => _balanceStaticData;
        public BallChainStaticData BallChainConfig => _ballChainConfig;
        public BallStaticData BallConfig => _ballConfig;
        public List<ChapterStaticData> Chapters => _chapterStaticDatas;
        
        public void LoadData()
        {
            _gameStaticData = Resources
                .Load<GameStaticData>(ResourcePath.GameConfigPath);
            
            _balanceStaticData = Resources
                .Load<BalanceStaticData>(ResourcePath.GameBalancePath);

            _gameplayInputConfig = Resources.Load<GameplayInputStaticData>(ResourcePath.GameplayInputConfigPath);
            if (_gameplayInputConfig == null)
                throw new InvalidOperationException("Gameplay input config is missing.");

            _ballConfig = Resources.Load<BallStaticData>(ResourcePath.BallConfigPath);
            if (_ballConfig == null)
                throw new InvalidOperationException("Ball appearance config is missing.");

            _ballChainConfig = Resources
                .Load<BallChainStaticData>(ResourcePath.BallChainConfigPath);
            
            _windowConfigs = Resources
                .Load<WindowStaticData>(ResourcePath.WindowsStaticDataPath)
                .Configs.ToDictionary(x => x.WindowTypeId, x => x);
            
            _chapterStaticDatas = Resources
                .LoadAll<ChapterStaticData>(ResourcePath.ChaptersStaticDataPath)
                .Where(chapter => chapter.Levels.Count > 0)
                .OrderBy(chapter => chapter.ChapterId)
                .ToList();
        }

        public WindowConfig ForWindow(WindowTypeId windowTypeId) => _windowConfigs[windowTypeId];

        public LevelStaticData ForLevel(int chapterId, int levelId)
        {
            LevelStaticData level = ForChapter(chapterId).Levels.Find(item => item.LevelId == levelId);
            return level != null ? level : throw new ArgumentOutOfRangeException(nameof(levelId),
                $"Chapter {chapterId} has no level with ID {levelId}.");
        }
        
        public ChapterStaticData ForChapter(int chapterId)
        {
            ChapterStaticData chapter = _chapterStaticDatas.Find(item => item.ChapterId == chapterId);
            return chapter != null ? chapter : throw new ArgumentOutOfRangeException(nameof(chapterId),
                $"There is no playable chapter with ID {chapterId}.");
        }

        public BallChainDTO GetBallChainDTO()
        {
            var ballChainDTO = new BallChainDTO();
            ballChainDTO.DurationSpawnBall = _ballChainConfig.DurationSpawnBall;
            ballChainDTO.MoveSpeed = _ballChainConfig.MoveSpeed;
            ballChainDTO.SpacingBalls = _ballChainConfig.SpacingBalls;
            ballChainDTO.DurationMovingOffset = _ballChainConfig.DurationMovingOffset;
            ballChainDTO.ChainSpringStrength = _ballChainConfig.ChainSpringStrength;
            ballChainDTO.ChainGapSpringStrength = _ballChainConfig.ChainGapSpringStrength;
            ballChainDTO.CatchupSpeedMultiplier = _ballChainConfig.CatchupSpeedMultiplier;
            ballChainDTO.CollisionThreshold = _ballChainConfig.CollisionThreshold;
            ballChainDTO.MatchingCount = _ballChainConfig.MatchingCount;
            ballChainDTO.MinParticleSpeed = _ballChainConfig.MinParticleSpeed;
            ballChainDTO.MaxParticleSpeed = _ballChainConfig.MaxParticleSpeed;
            ballChainDTO.BaseColorWidget = _ballChainConfig.BaseColorWidget;
            ballChainDTO.SetToSpawnWidget = _ballChainConfig.SetToSpawnWidget;
            ballChainDTO.TimeToSpawnWidget = _ballChainConfig.TimeToSpawnWidget;
            ballChainDTO.PercentToDetectionLose = _ballChainConfig.PercentToDetectionLose;
            ballChainDTO.LoseSpeedMultiplier = _ballChainConfig.LoseSpeedMultiplier;
            return ballChainDTO;
        }
    }
}