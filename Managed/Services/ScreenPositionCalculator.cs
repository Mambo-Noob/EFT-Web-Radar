using AncientMountain.Managed.Data;
using SkiaSharp;
using System.Drawing;
using System.Numerics;

namespace AncientMountain.Managed.Services
{
    public static class ScreenPositionCalculator
    {
        private const int VIEWPORT_TOLERANCE = 800;
        /// <summary>
        /// Calculates the position of an item on the player's screen
        /// </summary>
        /// <param name="player">The player</param>
        /// <param name="item">The item to calculate screen position for</param>
        /// <param name="screenWidth">Width of the screen in pixels</param>
        /// <param name="screenHeight">Height of the screen in pixels</param>
        /// <param name="fieldOfView">Camera field of view in degrees</param>
        /// <returns>Vector2 with x,y screen coordinates or null if item is behind player</returns>
        public static Vector2? GetItemScreenPosition(WebRadarPlayer player, WebRadarLoot item, int screenWidth, int screenHeight, float fieldOfView = 75f)
        {
            // Convert Unity Vector3 to System.Numerics.Vector3 if needed
            Vector3 itemPosition = ConvertToSystemVector(item.Position);

            // Calculate relative position (item position relative to player)
            Vector3 relativePosition = itemPosition - player.Position;

            // Create rotation matrix based on player's rotation
            float rotationRadians = player.MapRotation * (float)Math.PI / 180f;

            // Rotate the relative position based on player's orientation
            float rotatedX = relativePosition.X * (float)Math.Cos(rotationRadians) - relativePosition.Z * (float)Math.Sin(rotationRadians);
            float rotatedZ = relativePosition.X * (float)Math.Sin(rotationRadians) + relativePosition.Z * (float)Math.Cos(rotationRadians);

            // Construct the rotated position
            Vector3 rotatedPosition = new Vector3(rotatedX, relativePosition.Y, rotatedZ);

            // Check if item is in front of player (positive Z indicates in front in most 3D coordinate systems)
            if (rotatedPosition.Z <= 0)
            {
                // Item is behind player, not visible on screen
                return null;
            }

            // Calculate field of view in radians
            float fovRadians = fieldOfView * (float)Math.PI / 180f;

            // Calculate screen coordinates
            float screenX = (rotatedPosition.X / (rotatedPosition.Z * (float)Math.Tan(fovRadians / 2f))) * screenWidth / 2f + screenWidth / 2f;
            float screenY = (-rotatedPosition.Y / (rotatedPosition.Z * (float)Math.Tan(fovRadians / 2f))) * screenHeight / 2f + screenHeight / 2f;

            return new Vector2(screenX, screenY);
        }

        /// <summary>
        /// Helper method to convert Unity Vector3 to System.Numerics.Vector3 if needed
        /// </summary>
        private static Vector3 ConvertToSystemVector(Vector3 unityVector)
        {
            // If item.Position is already System.Numerics.Vector3, you can remove this method
            // If it's UnityEngine.Vector3, you'll need to convert it
            return unityVector; // Assuming it's already System.Numerics.Vector3
        }

        /// <summary>
        /// Checks if the item is visible on screen
        /// </summary>
        public static bool IsItemOnScreen(Vector2? screenPosition, int screenWidth, int screenHeight)
        {
            if (!screenPosition.HasValue)
                return false;

            Vector2 position = screenPosition.Value;
            return position.X >= 0 && position.X <= screenWidth &&
                   position.Y >= 0 && position.Y <= screenHeight;
        }

        /// <summary>
        /// Calculates distance between player and item
        /// </summary>
        public static float GetDistanceToItem(WebRadarPlayer player, WebRadarLoot item)
        {
            Vector3 itemPosition = ConvertToSystemVector(item.Position);
            return Vector3.Distance(player.Position, itemPosition);
        }

        public static bool WorldToScreen(Vector3 playerPosition, Vector2 playerRotation, Vector3 itemPosition,
                                         out SKPoint screenPos, float fov, float aspectRatio,
                                         SKPoint viewportCenter, Rectangle viewport,
                                         bool isScoped = false, bool onScreenCheck = false, bool useTolerance = false)
        {

            // Calculate relative position of the item from player
            Vector3 relativePos = itemPosition - playerPosition;

            // Convert yaw and pitch from Vector2 to view vectors
            // playerRotation.X = yaw (left/right), playerRotation.Y = pitch (up/down)
            float yaw = playerRotation.X * (MathF.PI / 180f); // Convert to radians if using ToRadians()
            float pitch = playerRotation.Y * (MathF.PI / 180f);   // Convert to radians if using ToRadians()

            // Calculate the view vectors from yaw and pitch angles
            // Forward vector (negative Z) - matches your direction conversion
            Vector3 forward = new Vector3(
                (float)(Math.Cos(pitch) * Math.Sin(yaw)),
                (float)Math.Sin(-pitch), // Negative pitch as in your code
                (float)(Math.Cos(pitch) * Math.Cos(yaw))
            );
            forward = Vector3.Normalize(forward);

            // Right vector (X axis) - cross product of world up and forward
            Vector3 right = Vector3.Cross(Vector3.UnitY, forward);
            right = Vector3.Normalize(right);

            // Up vector (Y axis) - cross product of forward and right
            Vector3 up = Vector3.Cross(forward, right);
            up = Vector3.Normalize(up);

            // Calculate dot products to get coordinates in camera space
            float w = Vector3.Dot(-forward, relativePos) + 1.0f; // Equivalent to M44
            if (w < 0.098f)
            {
                screenPos = default;
                return false;
            }

            float x = Vector3.Dot(right, relativePos); // Equivalent to M14
            float y = Vector3.Dot(up, relativePos);    // Equivalent to M24

            // Handle scoped view if needed
            if (isScoped)
            {
                float angleRadHalf = (MathF.PI / 180f) * fov * 0.5f;
                float angleCtg = MathF.Cos(angleRadHalf) / MathF.Sin(angleRadHalf);
                x /= angleCtg * aspectRatio * 0.5f;
                y /= angleCtg * 0.5f;
            }

            // Project to screen coordinates
            screenPos = new SKPoint
            {
                X = viewportCenter.X * (1f + x / w),
                Y = viewportCenter.Y * (1f - y / w)
            };

            // Check if the point is visible on screen
            if (onScreenCheck)
            {
                int left = useTolerance ? viewport.Left - VIEWPORT_TOLERANCE : viewport.Left;
                int rightl = useTolerance ? viewport.Right + VIEWPORT_TOLERANCE : viewport.Right;
                int top = useTolerance ? viewport.Top - VIEWPORT_TOLERANCE : viewport.Top;
                int bottom = useTolerance ? viewport.Bottom + VIEWPORT_TOLERANCE : viewport.Bottom;

                // Check if the screen position is within the screen boundaries
                if (screenPos.X < left || screenPos.X > rightl ||
                    screenPos.Y < top || screenPos.Y > bottom)
                {
                    screenPos = default;
                    return false;
                }
            }

            return true;
        }
    }
}
