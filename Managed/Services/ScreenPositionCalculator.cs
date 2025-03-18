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

            // Dynamically calculate FOV
            float effectiveFOV = horizontalFOV;
            if (entity.Position.Z < lPlayer.Position.Z)
            {
                // Slightly reduce FOV for objects behind the player's forward direction
                effectiveFOV *= 0.95f;
            }

            // Calculate FOV and aspect ratio
            float hFovRadians = effectiveFOV * (float)Math.PI / 180.0f;
            float aspectRatio = (float)screenWidth / screenHeight;
            float vFovRadians = 2.0f * (float)Math.Atan(Math.Tan(hFovRadians / 2.0f) / aspectRatio);

            // Calculate screen position using perspective projection
            float tanHalfHFov = (float)Math.Tan(hFovRadians / 2.0f);
            float tanHalfVFov = (float)Math.Tan(vFovRadians / 2.0f);

            // Calculate projected coordinates
            float xProj = xView / (zView * tanHalfHFov);
            float yProj = yView / (zView * tanHalfVFov);

            // Apply a vertical correction factor based on pitch angle and distance
            float pitchAngle = (float)Math.Asin(viewDirection.Y);
            float verticalCorrection = 0;
            float horizontalCorrection = 0;

            // Enhanced distance-based correction
            float distanceFactor = Math.Max(0.1f, Math.Min(1.0f, 50.0f / distanceToEntity));
            float scaledCorrection = 0.01f + (1.0f - distanceFactor) * 0.1f;

            // Apply stronger correction for distant objects
            if (Math.Abs(pitchAngle) > 0.05f)
            {
                float pitchFactor = (float)Math.Sin(pitchAngle);
                verticalCorrection = pitchFactor * scaledCorrection;

                // Apply the correction to both X and Y for distant objects
                horizontalCorrection = pitchFactor * scaledCorrection * 0.3f;

                // Apply more aggressive correction for very distant objects
                if (distanceToEntity > 200.0f)
                {
                    verticalCorrection *= 1.5f;
                    horizontalCorrection *= 1.3f;
                }

                yProj -= verticalCorrection;
                xProj -= horizontalCorrection * (xProj > 0 ? 1 : -1); // Push toward center horizontally
            }

            // Apply additional perspective correction for very distant objects
            if (distanceToEntity > 100.0f)
            {
                // Pull very distant objects slightly toward screen center
                float distanceScale = Math.Min(3.0f, distanceToEntity / 100.0f);
                float pullFactor = 0.05f * (distanceScale - 1.0f);
                xProj *= (1.0f - pullFactor);
                yProj *= (1.0f - pullFactor);
            }

            // Check if the projected point is within the view frustum
            bool isFullyVisible = Math.Abs(xProj) <= 1.0f && Math.Abs(yProj) <= 1.0f;

            // Calculate edge padding based on icon size and distance
            float edgePadding = iconSize * 0.5f; // Half the icon size as padding
            float edgeDistanceFactor = Math.Min(1.0f, 100.0f / Math.Max(distanceToEntity, 1.0f));
            edgePadding *= (1.0f + (1.0f - edgeDistanceFactor) * 2.0f); // Increase padding for distant objects

            // Convert to raw screen coordinates
            float rawX = (xProj + 1.0f) * 0.5f * screenWidth;
            float rawY = (1.0f - (yProj + 1.0f) * 0.5f) * screenHeight;

            // Clamp to screen edges with padding
            screenPos.X = Math.Clamp(rawX, edgePadding, screenWidth - edgePadding);
            screenPos.Y = Math.Clamp(rawY, edgePadding, screenHeight - edgePadding);

            // For very distant objects outside frustum, we still want to show them at the edges
            if (!isFullyVisible && distanceToEntity > 150.0f)
            {
                // Calculate normalized direction vector to the entity on screen
                float dx = xProj;
                float dy = yProj;
                float magnitude = (float)Math.Sqrt(dx * dx + dy * dy);

                if (magnitude > 0)
                {
                    // Normalize
                    dx /= magnitude;
                    dy /= magnitude;

                    // Position at screen edge based on direction
                    rawX = screenWidth * 0.5f + dx * (screenWidth * 0.5f - edgePadding);
                    rawY = screenHeight * 0.5f - dy * (screenHeight * 0.5f - edgePadding);

                    screenPos.X = rawX;
                    screenPos.Y = rawY;
                }
            }

            return true; // Always return true since we're now showing icons even at edges
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
