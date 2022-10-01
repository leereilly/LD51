using System.Collections.Generic;
using Godot;
using GodotUtilities;

namespace Game.GameObject
{
    public class Grapple : Node2D
    {
        private const int SEGMENT_LENGTH = 4;
        private const float AMP_DECAY = 50f;
        private const float FREQUENCY_DECAY = 3f;

        private float waveAmplitude = 0f;
        private float waveFrequency = 0f;
        private Vector2 connectedPosition;

        [Node]
        private Line2D line2d;

        public override void _Notification(int what)
        {
            if (what == NotificationInstanced)
            {
                this.WireNodes();
            }
        }

        // TODO: amp should be tween
        public override void _Process(float delta)
        {
            waveAmplitude = Mathf.Max(waveAmplitude - (delta * AMP_DECAY), 0f);
            waveFrequency = Mathf.Min(waveFrequency + (delta * FREQUENCY_DECAY), 8f);
            UpdateGrapple();
        }

        public void Shoot()
        {
            var direction = this.GetMouseDirection();
            ClearGrapple();
            var raycast = GetTree().Root.World2d.DirectSpaceState.Raycast(GlobalPosition, direction * 1000f, null, 1 << 0, true, false);
            if (raycast != null)
            {
                connectedPosition = raycast.Position;
                waveAmplitude = 15f;
                waveFrequency = 4f;
                UpdateGrapple();
            }
        }

        private void UpdateGrapple()
        {
            ClearGrapple();

            var straightLineDirection = (connectedPosition - GlobalPosition).Normalized();
            var perpendicularDirection = straightLineDirection.Perpendicular();
            var length = (connectedPosition - GlobalPosition).Length();
            var segments = Mathf.CeilToInt(length / SEGMENT_LENGTH);

            var initialPoint = GlobalPosition;
            var strictPoints = new List<Vector2> {
                line2d.ToLocal(initialPoint)
            };
            var wavyPoints = new List<Vector2> {
                line2d.ToLocal(initialPoint)
            };

            for (int i = 1; i < segments; i++)
            {
                var previousStrictPoint = strictPoints[i - 1];
                // var previousWavyPoint = wavyPoints[i - 1];
                var strictPoint = previousStrictPoint + (straightLineDirection * SEGMENT_LENGTH);

                var wavyPoint = strictPoint + (perpendicularDirection * Mathf.Sin(i * (Mathf.Pi / waveFrequency)) * waveAmplitude);

                strictPoints.Add(strictPoint);
                wavyPoints.Add(wavyPoint);
            }

            line2d.Points = wavyPoints.ToArray();
        }

        private void ClearGrapple()
        {
            line2d.ClearPoints();
        }
    }
}