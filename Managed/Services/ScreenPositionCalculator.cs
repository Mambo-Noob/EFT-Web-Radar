using AncientMountain.Managed.Data;
using System.Numerics;

namespace AncientMountain.Managed.Services
{
    public static class ScreenPositionCalculator
    {
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
    }
}
