using AncientMountain.Managed.Data;
using AncientMountain.Pages;
using SkiaSharp;
using System.Drawing;
using System.Numerics;

namespace AncientMountain.Managed.Services
{
    public static class ScreenPositionCalculator
    {
        private const int VIEWPORT_TOLERANCE = 800;

        // Helper method to get screen position




        // Alternative WorldToScreen method that doesn't rely on _viewMatrix property
        public static bool WorldToScreen(Vector3 worldPos, out SKPoint scrPos, WebRadarPlayer player, bool onScreenCheck = false, bool useTolerance = false)
        {
            var Viewport = new Rectangle(0, 0, 1920, 1080);
            var ViewportCenter = new SKPoint(Viewport.Width / 2f, Viewport.Height / 2f);

            // Create view matrix based on player position and rotation
            float pitch = player.Rotation.Y * (MathF.PI / 180f);  // Convert to radians
            float yaw = player.Rotation.X * (MathF.PI / 180f);    // Convert to radians

            // Forward vector (direction the player is facing)
            Vector3 forward = new Vector3(
                MathF.Cos(pitch) * MathF.Cos(yaw),
                MathF.Cos(pitch) * MathF.Sin(yaw),
                MathF.Sin(pitch)
            );

            // Right vector (perpendicular to forward, pointing right)
            Vector3 right = new Vector3(
                MathF.Sin(yaw - MathF.PI / 2),
                MathF.Cos(yaw - MathF.PI / 2),
                0
            );

            // Up vector (perpendicular to forward and right)
            Vector3 up = Vector3.Cross(right, forward);

            // Calculate relative position from player to world position
            Vector3 relativePos = worldPos - player.Position;

            // Calculate transformed coordinates using dot products
            float w = Vector3.Dot(forward, relativePos) + 1.0f; // +1.0f is equivalent to M44

            if (w < 0.098f)
            {
                scrPos = default;
                return false;
            }

            float x = Vector3.Dot(right, relativePos);
            float y = Vector3.Dot(up, relativePos);

            // Handle scoped adjustments (if player has these properties)
            float fov = 90.0f; // Default FOV - replace with player.FOV if available
            float aspect = 16.0f / 9.0f; // Default aspect ratio - replace with player.AspectRatio if available
            bool isScoped = false; // Default - replace with player.IsScoped if available

            if (isScoped)
            {
                float angleRadHalf = (MathF.PI / 180f) * fov * 0.5f;
                float angleCtg = MathF.Cos(angleRadHalf) / MathF.Sin(angleRadHalf);

                x /= angleCtg * aspect * 0.5f;
                y /= angleCtg * 0.5f;
            }

            // Calculate screen position
            var center = ViewportCenter; // Use existing ViewportCenter
            scrPos = new SKPoint
            {
                X = center.X * (1f + x / w),
                Y = center.Y * (1f - y / w)
            };

            // Optional on-screen check
            if (onScreenCheck)
            {
                int left = useTolerance ? Viewport.Left - VIEWPORT_TOLERANCE : Viewport.Left;
                int rightl = useTolerance ? Viewport.Right + VIEWPORT_TOLERANCE : Viewport.Right;
                int top = useTolerance ? Viewport.Top - VIEWPORT_TOLERANCE : Viewport.Top;
                int bottom = useTolerance ? Viewport.Bottom + VIEWPORT_TOLERANCE : Viewport.Bottom;

                if (scrPos.X < left || scrPos.X > rightl || scrPos.Y < top || scrPos.Y > bottom)
                {
                    scrPos = default;
                    return false;
                }
            }

            return true;
        }
    }
}
