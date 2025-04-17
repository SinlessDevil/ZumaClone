using UnityEngine;
using UnityEngine.UI;

namespace Code.UI.Game
{
    public class StartLevelInfoDisplayer : MonoBehaviour
    {
        private const string TriggerPlay = "Play";
        
        [SerializeField] private Animator _animator;
        [SerializeField] private Text _textLevelName;
        
        public bool IsActive { get; private set; }
        
        public void Initialize(string LevelName)
        {
            _textLevelName.text = LevelName;
        }
        
        public void Play()
        {
            IsActive = true;
            
            _animator.SetTrigger(TriggerPlay);
        }

        private void OnStopAnimation()
        {
            IsActive = false;
        }
    }   
}