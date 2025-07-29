using Code.StaticData.Levels;

namespace Code.UI.Menu.Windows.Map.Navigator
{
    public interface IChapterNavigator
    {
        int CurrentChapterIndex { get; }
        ChapterStaticData CurrentChapter { get; }
    
        bool CanSwipeLeft { get; }
        bool CanSwipeRight { get; }

        void SwipeLeft();
        void SwipeRight();
    }   
}