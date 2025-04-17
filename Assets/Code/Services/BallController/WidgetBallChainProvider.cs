using System.Collections.Generic;
using System.Linq;
using Code.Logic.Zuma.Balls;
using Code.Services.Providers.Widgets;
using PathCreation;
using UnityEngine;

namespace Code.Services.BallController
{
    public class WidgetBallChainProvider
    {
        private readonly IWidgetProvider _widgetProvider;
        private readonly PathCreator _pathCreator;

        public WidgetBallChainProvider(
            IWidgetProvider widgetProvider,
            PathCreator pathCreator)
        {
            _widgetProvider = widgetProvider;
            _pathCreator = pathCreator;
        }
        
        public void SetUpWidget(List<Ball> matchingBalls, Ball insertedBall, int count)
        {
            var totalDistance = matchingBalls.Sum(ball => _pathCreator.path.GetClosestDistanceAlongPath(ball.transform.position));
            var averageDistance = totalDistance / matchingBalls.Count;
            var position = _pathCreator.path.GetPointAtDistance(averageDistance);
            var color = insertedBall.Color;
            var countText = count.ToString();
            
            SetUpWidget(position, color, countText);
        }
        
        public void SetUpWidget(Vector3 position, Color color, string text)
        {
            var widget = _widgetProvider.GetWidget(position, Quaternion.identity);
            widget.SetText(text);
            widget.SetColor(color);
            widget.PlayAnimation();
        }
    }
}