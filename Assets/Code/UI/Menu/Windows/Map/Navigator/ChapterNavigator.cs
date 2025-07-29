using System;
using System.Collections.Generic;
using Code.Services.Levels;
using Code.StaticData.Levels;

namespace Code.UI.Menu.Windows.Map.Navigator
{
    public class ChapterNavigator : IChapterNavigator
    {
        private readonly ILevelService _levelService;
        private List<ChapterStaticData> _chapters;
        private int _currentChapterIndex;

        public ChapterNavigator(ILevelService levelService)
        {
            _levelService = levelService ?? throw new ArgumentNullException(nameof(levelService));
        
            _chapters = _levelService.GetAllChapters();
            _currentChapterIndex = _levelService.GetCurrentChapterIndex();
        }

        public int CurrentChapterIndex => _currentChapterIndex;
        public ChapterStaticData CurrentChapter => _chapters[_currentChapterIndex];

        public bool CanSwipeLeft => _currentChapterIndex > 0;
        public bool CanSwipeRight => _currentChapterIndex < _chapters.Count - 1;

        public void SwipeLeft()
        {
            if (CanSwipeLeft)
                _currentChapterIndex--;
        }

        public void SwipeRight()
        {
            if (CanSwipeRight)
                _currentChapterIndex++;
        }
    }   
}