namespace Code.UI.Menu.Windows
{
    public interface IWindowState
    {
        public void Constructor(BaseWindow window);
        public void Enter();
        public void Exit();
        public void Update();
    }
}