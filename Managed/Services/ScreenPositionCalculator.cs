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
            float yaw = playerRotation.X * (MathF.PI / 180f);
            float pitch = playerRotation.Y * (MathF.PI / 180f);

            // Create the camera basis vectors
            // Forward vector (camera's viewing direction)
            Vector3 forward = new Vector3(
                (float)(Math.Cos(pitch) * Math.Sin(yaw)),
                (float)Math.Sin(-pitch),  // Negative pitch because looking down is positive pitch
                (float)(Math.Cos(pitch) * Math.Cos(yaw))
            );
            forward = Vector3.Normalize(forward);

            // Right vector (perpendicular to forward, along the horizontal plane)
            // We want right to stay level with the horizon, so we use world up
            Vector3 worldUp = new Vector3(0, 1, 0); // World up is always Y+
            Vector3 right = Vector3.Cross(worldUp, forward);
            right = Vector3.Normalize(right);

            // Up vector (perpendicular to forward and right)
            // This ensures our up vector properly accounts for pitch
            Vector3 up = Vector3.Cross(forward, right);

            // Transform the object's position to camera space
            // z is the depth (distance to the object along the view direction)
            float z = Vector3.Dot(forward, relativePos);

            // If object is behind the camera, don't render it
            if (z < 0.098f)
            {
                screenPos = default;
                return false;
            }

            // Convert relative position to direction vector
            Vector3 dirToObject = Vector3.Normalize(relativePos);

            // Calculate the angle between forward vector and the direction to the object
            float dotProduct = Vector3.Dot(forward, dirToObject);

            // For a proper perspective view, the object must be in front of the camera
            // and within the view frustum defined by the FOV
            float cosHalfFOV = MathF.Cos((fov * 0.5f) * (MathF.PI / 180f));

            // If the object is outside our FOV, don't render it
            // For an object to be visible, the dot product must be greater than cosine of half FOV
            // This effectively creates a cone of vision
            if (dotProduct < cosHalfFOV)
            {
                screenPos = default;
                return false;
            }

            // Get x and y coordinates in camera space
            // These represent the object's position in the camera's coordinate system
            float x = Vector3.Dot(right, relativePos);
            float y = Vector3.Dot(up, relativePos);

            // Debug visualization to understand coordinate values
            // Console.WriteLine($"Camera-space coordinates - x: {x}, y: {y}, z: {z}");

            // This ensures proper perspective with accurate height representation

            // Convert to normalized device coordinates (NDC)
            // Handle FOV and aspect ratio
            if (isScoped)
            {
                float angleRadHalf = (MathF.PI / 180f) * fov * 0.5f;
                float angleCtg = MathF.Cos(angleRadHalf) / MathF.Sin(angleRadHalf);
                x = x / (z * angleCtg * aspectRatio * 0.5f);
                y = y / (z * angleCtg * 0.5f);
            } else
            {
                // Standard perspective projection
                float tanHalfFOV = MathF.Tan((fov * 0.5f) * (MathF.PI / 180f));
                x = x / (z * tanHalfFOV * aspectRatio);
                y = y / (z * tanHalfFOV);
            }

            // Convert NDC to screen coordinates
            screenPos = new SKPoint
            {
                X = viewportCenter.X * (1f + x),
                Y = viewportCenter.Y * (1f - y)  // Y is flipped in screen coords
            };

            // Check if the point is visible on screen
            if (onScreenCheck)
            {
                int left = useTolerance ? viewport.Left - VIEWPORT_TOLERANCE : viewport.Left;
                int right = useTolerance ? viewport.Right + VIEWPORT_TOLERANCE : viewport.Right;
                int top = useTolerance ? viewport.Top - VIEWPORT_TOLERANCE : viewport.Top;
                int bottom = useTolerance ? viewport.Bottom + VIEWPORT_TOLERANCE : viewport.Bottom;

                // Check if the screen position is within the screen boundaries
                if (screenPos.X < left || screenPos.X > right ||
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
