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

            // Get vector from player to entity
            Vector3 deltaVector = entity.Position - lPlayer.Position;

            // First check if entity is approximately in front of the player
            Vector3 viewDirection = Vector3.Normalize(RotationToDirection(lPlayer.Rotation));
            float dotProduct = Vector3.Dot(Vector3.Normalize(deltaVector), viewDirection);

            // If entity is behind player (not in field of view), return false
            if (dotProduct <= 0.0f)
            {
                return false;
            }

            // Calculate FOV in radians
            float hFovRadians = horizontalFOV * (float)Math.PI / 180.0f;

            // Calculate the forward, right, and up vectors for the view matrix
            Vector3 forward = viewDirection;
            Vector3 worldUp = new Vector3(0, 1, 0);

            // Avoid issues when looking straight up or down
            if (Math.Abs(Vector3.Dot(forward, worldUp)) > 0.99f)
            {
                worldUp = forward.Y > 0 ? new Vector3(0, 0, -1) : new Vector3(0, 0, 1);
            }

            Vector3 right = Vector3.Normalize(Vector3.Cross(worldUp, forward));
            Vector3 up = Vector3.Normalize(Vector3.Cross(forward, right));

            // Calculate view-space coordinates
            float rightDot = Vector3.Dot(deltaVector, right);
            float upDot = Vector3.Dot(deltaVector, up);
            float forwardDot = Vector3.Dot(deltaVector, forward);

            // Early exit if entity is behind camera
            if (forwardDot <= 0.1f)
            {
                return false;
            }

            // Calculate the angle from forward vector to the entity
            float horizontalAngle = (float)Math.Atan2(rightDot, forwardDot);
            float verticalAngle = (float)Math.Atan2(upDot, forwardDot);

            // Calculate aspect ratio and vertical FOV
            float aspectRatio = (float)screenWidth / screenHeight;
            float vFovRadians = 2.0f * (float)Math.Atan(Math.Tan(hFovRadians / 2.0f) / aspectRatio);

            // Check if entity is within field of view
            float halfHFov = hFovRadians / 2.0f;
            float halfVFov = vFovRadians / 2.0f;

            if (Math.Abs(horizontalAngle) > halfHFov || Math.Abs(verticalAngle) > halfVFov)
            {
                return false;
            }

            // Convert angles to normalized device coordinates (-1 to 1)
            float ndcX = horizontalAngle / halfHFov;
            float ndcY = verticalAngle / halfVFov;

            // Convert to screen coordinates
            screenPos.X = (ndcX + 1.0f) * 0.5f * screenWidth;
            screenPos.Y = (1.0f - (ndcY + 1.0f) * 0.5f) * screenHeight; // Flip Y axis

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
