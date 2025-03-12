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



        public static bool WorldToScreenWithPlayer(Vector3 worldPos, out SKPoint scrPos, WebRadarPlayer player, bool onScreenCheck = false, bool useTolerance = false)
        {
            var Viewport = new Rectangle(0, 0, 1920, 1080);
            var ViewportCenter = new SKPoint(Viewport.Width / 2f, Viewport.Height / 2f);
            scrPos = default;

            // Calculate view vectors based on player rotation
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

            // Check if object is in front of the player (dot product with forward vector is positive)
            // This ensures we only draw objects that are in the player's field of view
            float dotForward = Vector3.Dot(forward, relativePos);
            if (dotForward <= 0)
            {
                // Object is behind the player
                return false;
            }

            // FOV check (normalize the angle between forward and the object vector)
            // This ensures that objects outside the player's FOV aren't drawn
            float fov = 90.0f; // Default FOV - replace with player.FOV if available
            float relativeDistance = relativePos.Length();
            if (relativeDistance > 0.001f) // Avoid division by zero
            {
                Vector3 directionToObject = relativePos / relativeDistance;
                float angleCos = Vector3.Dot(forward, directionToObject);
                float angleRadians = MathF.Acos(angleCos);
                float angleDegrees = angleRadians * (180.0f / MathF.PI);

                if (angleDegrees > fov / 2)
                {
                    // Object is outside the player's FOV
                    return false;
                }
            }

            // W component acts as a depth value
            float w = dotForward;

            if (w < 0.098f)
            {
                // Too close to the near plane
                return false;
            }

            // Calculate screen coordinates using negative dot products for proper orientation
            float x = -Vector3.Dot(right, relativePos) / w;
            float y = -Vector3.Dot(up, relativePos) / w;

            // Handle scoped adjustments (if player has these properties)
            float aspect = 16.0f / 9.0f; // Default aspect ratio - replace with player.AspectRatio if available
            bool isScoped = false; // Default - replace with player.IsScoped if available

            if (isScoped)
            {
                float angleRadHalf = (MathF.PI / 180f) * fov * 0.5f;
                float angleCtg = MathF.Cos(angleRadHalf) / MathF.Sin(angleRadHalf);

                x /= angleCtg * aspect * 0.5f;
                y /= angleCtg * 0.5f;
            }

            // Calculate screen position - apply FOV scaling
            float fovScale = MathF.Tan((fov * 0.5f) * (MathF.PI / 180.0f));
            x = x / fovScale / aspect;
            y = y / fovScale;

            var center = ViewportCenter; // Use existing ViewportCenter
            scrPos = new SKPoint
            {
                X = center.X * (1.0f - x), // Invert X for correct left/right orientation
                Y = center.Y * (1.0f - y)  // Invert Y for correct up/down orientation
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
