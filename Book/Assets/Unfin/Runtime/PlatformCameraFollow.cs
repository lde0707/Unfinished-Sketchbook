using UnityEngine;

namespace Unfin
{
    // Motor-space target, never the animated head/cape. Units are map tiles.
    // Immediate horizontal tracking; short vertical damping starts on takeoff.
    public sealed class PlatformCameraFollow
    {
        public Vector2 Position { get; private set; }
        float verticalVelocity;

        public void Reset(Vector2 target, float halfHeight, float aspect, int width, int height)
        {
            Position = Clamp(target + Vector2.up, halfHeight, aspect, width, height);
            verticalVelocity = 0;
        }

        public Vector2 Tick(Vector2 target, Vector2 speed, bool grounded, bool flying,
            float dt, float halfHeight, float aspect, int width, int height)
        {
            if (dt <= 0) return Position;
            dt = Mathf.Min(dt, .1f);
            var desired = Clamp(target + Vector2.up, halfHeight, aspect, width, height);
            float smoothY = !grounded && !flying && speed.y > 0 ? .14f : .12f;
            float nextY = Mathf.SmoothDamp(Position.y, desired.y, ref verticalVelocity,
                smoothY, Mathf.Infinity, dt);

            // Safety window for long falls/dashes; keep the full sprite inside the frame.
            float safeY = Mathf.Max(.5f, halfHeight - .8f);
            float y = Mathf.Clamp(nextY, target.y - safeY, target.y + safeY);
            if (y != nextY) verticalVelocity = 0;
            Position = Clamp(new Vector2(desired.x,y), halfHeight, aspect, width, height);
            return Position;
        }

        static Vector2 Clamp(Vector2 p, float halfHeight, float aspect, int width, int height)
        {
            float halfWidth = halfHeight * aspect;
            return new Vector2(width <= halfWidth * 2 ? width * .5f : Mathf.Clamp(p.x, halfWidth, width - halfWidth),
                height <= halfHeight * 2 ? -height * .5f : Mathf.Clamp(p.y, -height + halfHeight, -halfHeight));
        }
    }
}
