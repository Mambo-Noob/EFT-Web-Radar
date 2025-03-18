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

            // Get the vector from player to entity
            Vector3 playerToEntity = entity.Position - lPlayer.Position;
            float distanceToEntity = playerToEntity.Length();

            // If entity is too close, calculations might be unstable
            if (distanceToEntity < 0.001f)
                return false;

            // Calculate the normalized direction to entity
            Vector3 directionToEnemy = playerToEntity / distanceToEntity;

            // Get the player's view vectors - using consistent world up vector
            Vector3 worldUp = new Vector3(0, 1, 0);
            Vector3 lPlayerForward = Vector3.Normalize(RotationToDirection(lPlayer.Rotation));

            // Ensure the forward vector isn't nearly parallel to world up
            if (Math.Abs(Vector3.Dot(lPlayerForward, worldUp)) > 0.9999f)
            {
                // Use a different temporary up vector if looking straight up/down
                worldUp = new Vector3(0, 0, 1);
            }

            // Calculate right vector (player's view right direction)
            Vector3 lPlayerRight = -Vector3.Normalize(Vector3.Cross(lPlayerForward, worldUp));

            // Calculate true up vector (player's view up direction)
            Vector3 lPlayerUp = -Vector3.Normalize(Vector3.Cross(lPlayerRight, lPlayerForward));

            // Convert horizontal FOV to radians
            float hFovRad = horizontalFOV * (float)Math.PI / 180f;

            // Calculate vertical FOV based on aspect ratio
            float aspectRatio = (float)screenWidth / screenHeight;
            float vFovRad = 2 * (float)Math.Atan(Math.Tan(hFovRad / 2) / aspectRatio);

            // Calculate dot products to determine the position in view space
            float dotForward = Vector3.Dot(directionToEnemy, lPlayerForward);

            // If entity is behind player, don't render
            if (dotForward <= 0.001f)
                return false;

            // Calculate normalized screen coordinates
            float dotRight = Vector3.Dot(directionToEnemy, lPlayerRight);
            float dotUp = Vector3.Dot(directionToEnemy, lPlayerUp);

            // Convert to tangent space
            float tanX = dotRight / dotForward;
            float tanY = dotUp / dotForward;

            // Calculate normalized device coordinates based on FOV
            float ndcX = tanX / (float)Math.Tan(hFovRad / 2);
            float ndcY = tanY / (float)Math.Tan(vFovRad / 2);

            // Check if the position is within the screen bounds
            if (Math.Abs(ndcX) > 1 || Math.Abs(ndcY) > 1)
                return false;

            // Convert NDC to screen coordinates (pixel coordinates)
            screenPos.X = (ndcX + 1) * screenWidth / 2;
            screenPos.Y = (1 - ndcY) * screenHeight / 2; // Invert Y as screen coordinates go from top to bottom

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
