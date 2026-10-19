using UnityEngine;

namespace Unfin
{
    // HTML logical pixels per second; one map tile =16 logical pixels.
    public static class PlayerMotion
    {
        public const float GroundSpeed=88;
        public const float AirSpeed=105.6f;
        public const float JumpSpeed=304.245f;
        // Keep gravity fixed: reducing launch speed now lowers jump height by 15%.
        public const float Gravity=1350*(330f/425)*(330f/425);
        public const float FallSpeed=378.4f;
        public const float DashSpeed=244.8f;
        public const float DashDuration=.2f;
        public const float DashCooldown=.42f; // Same220ms recovery after the dash.
        public const float RunPlaybackRate=GroundSpeed/125;
        public const float WallJumpSpeed=420*(JumpSpeed/425);
        public const float WallJumpHorizontal=154;
        public const float WallRegrabDelay=.045f;
        public const float WallReturnPush=90;
        public const float WallReturnAcceleration=28;
        public static readonly float JumpAngle=Mathf.Atan2(425,155)*Mathf.Rad2Deg-10;
        public static readonly float JumpHorizontal=JumpSpeed/Mathf.Tan(JumpAngle*Mathf.Deg2Rad);
    }
}
