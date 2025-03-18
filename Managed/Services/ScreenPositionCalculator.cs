using AncientMountain.Managed.Data;
using SkiaSharp;
using System.Drawing;
using System.Numerics;

namespace AncientMountain.Managed.Services
{
    public static class ScreenPositionCalculator
    {
        public static bool IsFacingTarget(WebRadarPlayer lPlayer, IEntity entity, float? maxDist = null)
        {
            var distance = Vector3.Distance(lPlayer.Position, entity.Position);
            if (maxDist is float maxDistFloat && distance > maxDistFloat)
                return false;

            // Calculate the 3D vector from enemy to player (including vertical component)
            Vector3 directionToTarget = Vector3.Normalize(entity.Position - lPlayer.Position);

            // Convert enemy rotation to a direction vector
            Vector3 sourceDirection = Vector3.Normalize(RotationToDirection(lPlayer.Rotation));

            // Calculate the angle between enemy direction and the direction to the player
            float dotProduct = Vector3.Dot(sourceDirection, directionToTarget);
            float angle = (float)Math.Acos(dotProduct); // Result in radians

            // Convert angle to degrees for easier interpretation (optional)
            float angleInDegrees = angle * (180f / (float)Math.PI);

            float angleThreshold = 31.3573f - 3.51726f * MathF.Log(MathF.Abs(0.626957f - 15.6948f * distance)); // Max degrees variance based on distance variable
            if (angleThreshold < 1f)
                angleThreshold = 1f; // Non linear equation, handle low/negative results

            return angleInDegrees <= angleThreshold;
        }

        //Acting as the lPlayer, get position of other player/enemy
        public static bool WorldToScreenPositionOnEnemyView(out SKPoint screenPos, IEntity entity, WebRadarPlayer lPlayer, int screenWidth = 2560, int screenHeight = 1440, float horizontalFOV = 70f)
        {
            screenPos = new SKPoint(0, 0);

            // Get the vector from player to entity in world space
            Vector3 playerToEntity = entity.Position - lPlayer.Position;

            // Get player's view direction
            Vector3 lPlayerForward = Vector3.Normalize(RotationToDirection(lPlayer.Rotation));

            // Calculate view matrix vectors
            // Use a consistent world up vector
            Vector3 worldUp = new Vector3(0, 1, 0);

            // Handle the case when looking directly up/down
            if (Math.Abs(Vector3.Dot(lPlayerForward, worldUp)) > 0.99f)
            {
                worldUp = new Vector3(1, 0, 0); // Use a different up vector
            }

            // Right vector is perpendicular to both forward and world up
            Vector3 lPlayerRight = Vector3.Normalize(Vector3.Cross(worldUp, lPlayerForward));

            // Up vector must be perpendicular to both forward and right
            Vector3 lPlayerUp = Vector3.Normalize(Vector3.Cross(lPlayerForward, lPlayerRight));

            // Project the player-to-entity vector onto the view space
            float viewX = Vector3.Dot(playerToEntity, lPlayerRight);
            float viewY = Vector3.Dot(playerToEntity, lPlayerUp);
            float viewZ = Vector3.Dot(playerToEntity, lPlayerForward);

            // Entity is behind the camera
            if (viewZ <= 0.01f)
                return false;

            // Convert FOV to radians
            float hFovRad = horizontalFOV * (float)Math.PI / 180f;
            float aspectRatio = (float)screenWidth / screenHeight;

            // Calculate vertical FOV
            float vFovRad = 2 * (float)Math.Atan(Math.Tan(hFovRad / 2) / aspectRatio);

            // Calculate NDC coordinates using direct perspective projection
            float ndcX = viewX / (viewZ * (float)Math.Tan(hFovRad / 2));
            float ndcY = viewY / (viewZ * (float)Math.Tan(vFovRad / 2));

            // If entity is outside the view frustum, don't render
            if (Math.Abs(ndcX) > 1.0f || Math.Abs(ndcY) > 1.0f)
                return false;

            // Convert NDC to screen coordinates
            screenPos.X = (ndcX + 1.0f) * 0.5f * screenWidth;
            screenPos.Y = (1.0f - ndcY) * 0.5f * screenHeight;

            return true;
        }

        private static Vector3 RotationToDirection(Vector2 rotation)
        {
            // Convert rotation (yaw, pitch) to a direction vector
            // This might need adjustments based on how you define rotation
            float yaw = (float)rotation.X.ToRadians();
            float pitch = (float)rotation.Y.ToRadians();
            Vector3 direction;
            direction.X = (float)(Math.Cos(pitch) * Math.Sin(yaw));
            direction.Y = (float)Math.Sin(-pitch); // Negative pitch because in Unity, as pitch increases, we look down
            direction.Z = (float)(Math.Cos(pitch) * Math.Cos(yaw));

            return Vector3.Normalize(direction);
        }
    }
}
