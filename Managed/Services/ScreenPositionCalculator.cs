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
            float distanceToEntity = deltaVector.Length();

            // Get player's view orientation
            Vector3 viewDirection = Vector3.Normalize(RotationToDirection(lPlayer.Rotation));

            // Calculate the basis vectors for the view matrix
            Vector3 forward = viewDirection;
            Vector3 worldUp = new Vector3(0, 1, 0);
            Vector3 right = Vector3.Normalize(Vector3.Cross(worldUp, forward));
            Vector3 up = Vector3.Normalize(Vector3.Cross(right, forward)); // Note the order

            // Calculate view-space coordinates
            float xView = Vector3.Dot(deltaVector, right);
            float yView = Vector3.Dot(deltaVector, up);
            float zView = Vector3.Dot(deltaVector, forward);

            // If entity is behind camera, don't render
            if (zView <= 0.1f)
            {
                return false;
            }

            // Calculate FOV and aspect ratio
            float hFovRadians = horizontalFOV * (float)Math.PI / 180.0f;
            float aspectRatio = (float)screenWidth / screenHeight;
            float vFovRadians = 2.0f * (float)Math.Atan(Math.Tan(hFovRadians / 2.0f) / aspectRatio);

            // Calculate screen position using perspective projection
            float tanHalfHFov = (float)Math.Tan(hFovRadians / 2.0f);
            float tanHalfVFov = (float)Math.Tan(vFovRadians / 2.0f);

            // Calculate projected coordinates
            float xProj = xView / (zView * tanHalfHFov);
            float yProj = yView / (zView * tanHalfVFov);

            // Apply a vertical correction factor based on pitch angle and distance
            // This helps with the "floating" effect when looking down at objects
            float pitchAngle = (float)Math.Asin(viewDirection.Y);
            float verticalCorrection = 0;

            if (Math.Abs(pitchAngle) > 0.1f) // If looking up or down significantly
            {
                // Calculate a correction factor based on pitch angle and distance
                // This dampens the vertical displacement for distant objects
                float pitchFactor = (float)Math.Sin(pitchAngle);
                float distanceFactor = Math.Min(1.0f, 10.0f / Math.Max(distanceToEntity, 1.0f));

                // Apply stronger correction when looking down at objects (when pitchAngle is negative)
                verticalCorrection = pitchFactor * distanceFactor * 0.5f;

                // Apply the correction to the Y projection
                yProj -= verticalCorrection;
            }

            // Check if the projected point is within the view frustum
            if (Math.Abs(xProj) > 1.0f || Math.Abs(yProj) > 1.0f)
            {
                return false;
            }

            // Convert to screen coordinates
            screenPos.X = (xProj + 1.0f) * 0.5f * screenWidth;
            screenPos.Y = (1.0f - (yProj + 1.0f) * 0.5f) * screenHeight;

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
