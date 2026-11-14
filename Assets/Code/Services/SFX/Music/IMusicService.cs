namespace Code.Services.SFX.Music
{
    public interface IMusicService
    {
        void CacheMusic();
        void Play(MusicType type, bool loop = true);
        void Pause();
        void Resume();
        void Stop();
        void SetVolume(float volume);
        float GetVolume();
    }
}