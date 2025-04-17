namespace Code.Infrastructure
{
    public interface ILoadingCurtain
    {
        public bool IsActive { get; }
        public void Show();
        public void Hide();
    }
}