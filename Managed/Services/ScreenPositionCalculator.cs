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

            // Calculate vector from player to item
            Vector3 relativePos = itemPosition - playerPosition;

            // Convert rotation angles to radians
            float yaw = playerRotation.X * (MathF.PI / 180f);
            float pitch = playerRotation.Y * (MathF.PI / 180f);

            // Calculate view matrix components
            // Forward vector
            Vector3 forward = new Vector3(
                (float)Math.Cos(pitch) * (float)Math.Sin(yaw),
                (float)Math.Sin(-pitch), // Negative pitch for looking down
                (float)Math.Cos(pitch) * (float)Math.Cos(yaw)
            );

            // Right vector - stays horizontal regardless of pitch
            Vector3 right = new Vector3(
                (float)Math.Cos(yaw + Math.PI / 2),
                0, // No Y component to keep it horizontal
                (float)Math.Sin(yaw + Math.PI / 2)
            );

            // Up vector - cross product of forward and right
            Vector3 up = Vector3.Cross(right, forward); // Note order swapped to get correct up direction

            // Normalize vectors
            forward = Vector3.Normalize(forward);
            right = Vector3.Normalize(right);
            up = Vector3.Normalize(up);

            // Project to view space
            float z = Vector3.Dot(forward, relativePos); // Depth

            // Check if behind the camera
            if (z < 0.1f)
            {
                screenPos = default;
                return false;
            }

            // Check FOV constraints
            float viewAngle = Vector3.Dot(forward, Vector3.Normalize(relativePos));
            float fovLimit = (float)Math.Cos((fov * 0.5f) * (Math.PI / 180f));

            if (viewAngle < fovLimit)
            {
                screenPos = default;
                return false;
            }

            // Project to camera space
            float x = Vector3.Dot(right, relativePos);
            float y = Vector3.Dot(up, relativePos);

            // Convert to NDC using perspective division
            float fovScale = (float)Math.Tan((fov * 0.5f) * (Math.PI / 180f));

            // Apply projection based on mode
            if (isScoped)
            {
                // Scoped view logic
                float angleRadHalf = (MathF.PI / 180f) * fov * 0.5f;
                float angleCtg = MathF.Cos(angleRadHalf) / MathF.Sin(angleRadHalf);
                x = x / (z * angleCtg * aspectRatio);
                y = y / (z * angleCtg);
            } else
            {
                // Standard projection
                x = x / (z * fovScale * aspectRatio);
                y = y / (z * fovScale);
            }

            // Convert to screen coordinates
            screenPos = new SKPoint
            {
                X = viewportCenter.X * (1f + x),
                Y = viewportCenter.Y * (1f - y) // Y is flipped in screen coordinates
            };

            // Screen bounds check
            if (onScreenCheck)
            {
                int left = useTolerance ? viewport.Left - VIEWPORT_TOLERANCE : viewport.Left;
                int rightl = useTolerance ? viewport.Right + VIEWPORT_TOLERANCE : viewport.Right;
                int top = useTolerance ? viewport.Top - VIEWPORT_TOLERANCE : viewport.Top;
                int bottom = useTolerance ? viewport.Bottom + VIEWPORT_TOLERANCE : viewport.Bottom;

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
