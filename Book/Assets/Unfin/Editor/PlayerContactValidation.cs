using System;
using System.IO;
using UnityEngine;
namespace Unfin.Editor
{
    public static class PlayerContactValidation
    {
        static void Check(bool ok, string message) { if (!ok) throw new Exception("Player contact: " + message); }
        public static void RunAndBuild()
        {
            PlatformSurfaceValidation.RepairAndValidate();
            MezzanineValidation.ValidateRoutes();
            var data = new WorldData { width = 20, height = 20 };
            var cells = new byte[400];
            var m = new GridMotor(data, cells);
            foreach (byte type in new byte[] { 1, 4, 6, 15, 16 })
            {
                Array.Clear(cells, 0, cells.Length);
                for (int x = 0; x < 20; x++) cells[10 * 20 + x] = type;
                m.x = 80; m.y = 120; m.vy = 160; m.vx = 0; m.dropTime = 0;
                for (int i = 0; i < 120; i++) { m.vy += 8; m.Move(1f / 120); }
                Check(m.grounded && Mathf.Abs(m.Bottom - 160) < .01f, "standing on type " + type);
                Check(Mathf.Abs(m.y + GridMotor.Height - 161) < .01f, "feet overlap recessed edge");
                m.vx = 80;
                for (int i = 0; i < 120; i++) { m.vy += 8; m.Move(1f / 120); Check(m.grounded, "tile seam snag"); }
                if (GridMotor.Platform(type))
                {
                    m.dropTime = .22f; m.y += 2; m.vy = 80; m.Move(.02f);
                    Check(!m.grounded && m.Bottom > 160, "drop through type " + type);
                }
            }
            Array.Clear(cells, 0, cells.Length);
            for (int y = 0; y < 20; y++) cells[y * 20 + 10] = 1;
            foreach (int side in new[] { 1, -1 })
            {
                m.x = side > 0 ? 140 : 180; m.y = 80; m.vy = 0; m.vx = side * 80;
                for (int i = 0; i < 60; i++) m.Move(1f / 120);
                Check(Mathf.Abs((side > 0 ? m.Right : m.Left) - (side > 0 ? 160 : 176)) < .01f, "wall collision");
                Check(m.TouchingWall() == side, "wall contact sensor");
                float x = m.x; m.SnapToWall(side); Check(Mathf.Abs(x - m.x) < .01f, "stable wall alignment");
            }
            PlayerWallValidation.Run();
            InkHazardValidation.Run();
            Directory.CreateDirectory("Validation");
            File.WriteAllText("Validation/player-contact.json", "{\"passed\":true,\"sideInsetLogicalPixels\":1,\"footInsetLogicalPixels\":1,\"checks\":[\"five floor types\",\"tile seams\",\"platform drop\",\"both walls\",\"wall jumps and regrab\",\"hazard respawn\"]}");
            Debug.Log("UNFIN_PLAYER_CONTACT_OK");
            WebGLBuilder.Build();
        }
    }
}
