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

            // Convert rotation to radians
            float yawRad = playerRotation.X * (MathF.PI / 180f);
            float pitchRad = playerRotation.Y * (MathF.PI / 180f);

            // We need to invert the yaw to correct the orientation issue
            yawRad = -yawRad; // Invert yaw

            // Build rotation matrix from yaw and pitch
            // For a typical FPS, we need to be careful about rotation order
            Matrix4x4 rotationMatrixYaw = Matrix4x4.CreateRotationY(yawRad);
            Matrix4x4 rotationMatrixPitch = Matrix4x4.CreateRotationX(pitchRad);
            Matrix4x4 rotationMatrix = rotationMatrixPitch * rotationMatrixYaw; // Apply yaw then pitch

            // Create view matrix (first translate to player position, then apply rotation)
            Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(-playerPosition);

            // The view matrix needs to be rotation * translation (in that order)
            // This ensures we first move the world so the player is at origin, then rotate
            Matrix4x4 viewMatrix = Matrix4x4.Multiply(rotationMatrix, translationMatrix);

            // Get viewport center and create projection matrix
            float tanHalfFovY = MathF.Tan(fov * 0.5f * (MathF.PI / 180f));
            float zNear = 0.1f;
            float zFar = 1000f;

            Matrix4x4 projectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
                fov * (MathF.PI / 180f), // FOV in radians
                aspectRatio,
                zNear,
                zFar
            );

            // Combine view and projection matrices
            Matrix4x4 viewProjectionMatrix = viewMatrix * projectionMatrix;

            // Transform world position to clip space
            Vector4 clipPos = Vector4.Transform(new Vector4(itemPosition, 1.0f), viewProjectionMatrix);

            // If behind camera or too close, don't render
            if (clipPos.W < 0.1f || clipPos.Z < 0)
            {
                screenPos = default;
                return false;
            }

            // Perspective division to get normalized device coordinates
            Vector3 ndcPos = new Vector3(
                clipPos.X / clipPos.W,
                clipPos.Y / clipPos.W,
                clipPos.Z / clipPos.W
            );

            // Check if point is within NDC bounds (-1 to 1)
            if (ndcPos.X < -1 || ndcPos.X > 1 || ndcPos.Y < -1 || ndcPos.Y > 1)
            {
                screenPos = default;
                return false;
            }

            // Convert to screen coordinates
            screenPos = new SKPoint
            {
                X = viewportCenter.X * (1 + ndcPos.X),
                Y = viewportCenter.Y * (1 - ndcPos.Y) // Y is flipped in screen space
            };

            // Screen bounds check
            if (onScreenCheck)
            {
                var left = useTolerance ? viewport.Left - VIEWPORT_TOLERANCE : viewport.Left;
                var right = useTolerance ? viewport.Right + VIEWPORT_TOLERANCE : viewport.Right;
                var top = useTolerance ? viewport.Top - VIEWPORT_TOLERANCE : viewport.Top;
                var bottom = useTolerance ? viewport.Bottom + VIEWPORT_TOLERANCE : viewport.Bottom;

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
