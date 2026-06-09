using Code.Logic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Code.PathCreation;
using UnityEngine;

namespace Code.Services.BallController
{
    public class ParticleChainHandler
    {
        private readonly PathCreator _pathCreator;
        private readonly BallChainDTO _ballChainDto;
        private readonly ChainState _chainState;

        public ParticleChainHandler(
            BallChainDTO ballChainDto,
            PathCreator pathCreator,
            ChainState chainState)
        {
            _ballChainDto = ballChainDto;
            _pathCreator = pathCreator;
            _chainState = chainState;
        }

        public async UniTask MoveParticleAlongPathAsync(ParticleSystemHolder particle)
        {
            particle.gameObject.SetActive(true);
            particle.Play();

            float distance = _chainState.HeadDistance;
            float pathLength = _pathCreator.path.length;

            if (pathLength <= 0)
            {
                particle.gameObject.SetActive(false);
                return;
            }

            float startProgress = distance / pathLength;
            float currentSpeed = Mathf.Lerp(_ballChainDto.MinParticleSpeed, _ballChainDto.MaxParticleSpeed, startProgress);

            float startTime = Time.time;
            float duration = pathLength / currentSpeed;

            while (distance < pathLength)
            {
                float progress = (Time.time - startTime) / duration;
                progress = Mathf.Clamp01(progress);
                distance = Mathf.Lerp(_chainState.HeadDistance, pathLength, progress);

                if (distance == pathLength)
                    break;

                particle.transform.position = _pathCreator.path.GetPointAtDistance(distance);
                await UniTask.Yield();
            }

            particle.transform.DOScale(Vector3.zero, 0.4f).SetEase(Ease.InQuart);
            await UniTask.Delay(650);
            particle.gameObject.SetActive(false);
            particle.transform.localScale = Vector3.one;
            particle.Stop();
        }
    }
}
