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

            // Get player's view matrix components
            Vector3 lPlayerForward = Vector3.Normalize(RotationToDirection(lPlayer.Rotation));
            Vector3 worldUp = new Vector3(0, 1, 0);

            // Handle edge case when looking directly up/down
            float upDot = Vector3.Dot(lPlayerForward, worldUp);
            if (Math.Abs(upDot) > 0.9999f)
            {
                worldUp = new Vector3(1, 0, 0); // Use a different up vector if looking straight up/down
            }

            // Calculate view matrix vectors
            Vector3 lPlayerRight = Vector3.Normalize(Vector3.Cross(worldUp, lPlayerForward));
            Vector3 lPlayerUp = Vector3.Normalize(Vector3.Cross(lPlayerForward, lPlayerRight));

            // Transform the point to view space
            // This is equivalent to multiplying by the inverse of the view matrix
            float viewX = Vector3.Dot(playerToEntity, lPlayerRight);
            float viewY = Vector3.Dot(playerToEntity, lPlayerUp);
            float viewZ = Vector3.Dot(playerToEntity, lPlayerForward);

            // If behind camera, don't render
            if (viewZ <= 0.001f)
                return false;

            // Convert horizontal FOV to radians
            float hFovRad = horizontalFOV * (float)Math.PI / 180f;
            float aspectRatio = (float)screenWidth / screenHeight;

            // Calculate the projection parameters
            float nearPlane = 0.1f; // Assuming a near plane distance

            // Calculate projection matrix factors
            float projectionX = 1.0f / (float)Math.Tan(hFovRad / 2);
            float projectionY = aspectRatio / (float)Math.Tan(hFovRad / 2);

            // Apply perspective division (z-divide)
            float ndcX = (viewX / viewZ) * projectionX;
            float ndcY = (viewY / viewZ) * projectionY;

            // Apply correct non-linear projection for edge-case correction
            // This applies a slight correction to reduce edge distortion
            float correctionFactor = 1.0f + (ndcX * ndcX + ndcY * ndcY) * 0.1f;
            ndcX /= correctionFactor;
            ndcY /= correctionFactor;

            // Check if the position is within the screen bounds
            if (Math.Abs(ndcX) > 1 || Math.Abs(ndcY) > 1)
                return false;

            // Convert NDC to screen coordinates (pixel coordinates)
            screenPos.X = (ndcX + 1) * screenWidth / 2;
            screenPos.Y = (1 - ndcY) * screenHeight / 2; // Invert Y for screen coordinates

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
