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
        public static bool WorldToScreenPositionOnEnemyView(out SKPoint screenPos, IEntity entity, WebRadarPlayer lPlayer, int screenWidth = 2560, int screenHeight = 1440, float horizontalFOV = 70f, float zoomFactor = 1.0f)
        {
            screenPos = new SKPoint(0, 0);

            // Apply zoom factor to FOV (smaller FOV = more zoom)
            float zoomedFOV = horizontalFOV / zoomFactor;

            // Create a proper view matrix using camera position and orientation
            Matrix4x4 viewMatrix = CreateViewMatrix(lPlayer.Position, lPlayer.Rotation);

            // Create a proper perspective projection matrix with zoomed FOV
            Matrix4x4 projMatrix = CreateProjectionMatrix(zoomedFOV, (float)screenWidth / screenHeight, 0.1f, 1000f);

            // Calculate the full view-projection matrix
            Matrix4x4 viewProjMatrix = Matrix4x4.Multiply(viewMatrix, projMatrix);

            // Transform entity position to clip space
            Vector3 entityPosition = entity.Position;
            Vector4 clipPos = Vector4.Transform(new Vector4(entityPosition.X, entityPosition.Y, entityPosition.Z, 1.0f), viewProjMatrix);

            // Check if behind camera
            if (clipPos.W <= 0.0f)
            {
                // For objects behind, we'll place indicators at screen edges
                PlaceOffScreenIndicator(out screenPos, entityPosition, lPlayer.Position, lPlayer.Rotation, screenWidth, screenHeight, zoomFactor);
                return false;
            }

            // Calculate normalized device coordinates by perspective division
            float invW = 1.0f / clipPos.W;
            float ndcX = clipPos.X * invW;
            float ndcY = clipPos.Y * invW;
            float ndcZ = clipPos.Z * invW;

            // Calculate distance for scaling
            float distance = Vector3.Distance(entityPosition, lPlayer.Position);

            // When zoomed in, the NDC bounds are effectively tighter
            // An object at NDC (0.5, 0.5) with zoom 2.0 would be at the edge of the view
            float zoomedNdcBound = 1.0f / zoomFactor;

            // Check if object is within zoomed NDC bounds
            bool isInView = ndcX >= -zoomedNdcBound && ndcX <= zoomedNdcBound &&
            ndcY >= -zoomedNdcBound && ndcY <= zoomedNdcBound &&
            ndcZ >= 0.0f && ndcZ <= 1.0f;

            if (!isInView)
            {
                // Handle off-screen indicators with proper edge clamping
                PlaceOffScreenIndicator(out screenPos, entityPosition, lPlayer.Position, lPlayer.Rotation, screenWidth, screenHeight, zoomFactor);
                return false;
            }

            // When zoomed in, we need to scale NDC coordinates to fill the screen
            // This maps the smaller zoomed NDC range (-zoomedNdcBound to zoomedNdcBound) to the full screen
            float scaledNdcX = ndcX / zoomedNdcBound;
            float scaledNdcY = ndcY / zoomedNdcBound;

            // Convert from scaled NDC to screen space
            float screenX = (scaledNdcX + 1.0f) * 0.5f * screenWidth;
            float screenY = (1.0f - (scaledNdcY + 1.0f) * 0.5f) * screenHeight;

            // Apply distance-based corrections (less correction needed when zoomed in)
            ApplyDistanceBasedCorrections(ref screenX, ref screenY, distance, scaledNdcX, scaledNdcY, screenWidth, screenHeight, zoomFactor);

            screenPos.X = screenX;
            screenPos.Y = screenY;

            return true;
        }

        private static Matrix4x4 CreateViewMatrix(Vector3 cameraPosition, Vector2 rotation)
        {
            // Convert rotation (yaw, pitch) to direction vectors
            float yawRad = (float)rotation.X.ToRadians();
            float pitchRad = (float)rotation.Y.ToRadians();

            // Calculate view direction
            Vector3 forward = new Vector3(
            (float)(Math.Cos(pitchRad) * Math.Sin(yawRad)),
            (float)Math.Sin(-pitchRad),
            (float)(Math.Cos(pitchRad) * Math.Cos(yawRad))
            );
            forward = Vector3.Normalize(forward);

            // Calculate right and up vectors
            Vector3 worldUp = new Vector3(0, 1, 0);
            Vector3 right = Vector3.Normalize(Vector3.Cross(worldUp, forward));
            Vector3 up = Vector3.Normalize(Vector3.Cross(forward, right));

            // Create view matrix components
            Matrix4x4 rotationMatrix = new Matrix4x4(
            right.X, up.X, forward.X, 0,
            right.Y, up.Y, forward.Y, 0,
            right.Z, up.Z, forward.Z, 0,
            0, 0, 0, 1
            );

            Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(-cameraPosition);

            // Combine rotation and translation
            return Matrix4x4.Multiply(translationMatrix, rotationMatrix);
        }

        private static Matrix4x4 CreateProjectionMatrix(float fovDegrees, float aspectRatio, float nearPlane, float farPlane)
        {
            float fovRadians = fovDegrees * (float)Math.PI / 180.0f;
            float yScale = 1.0f / (float)Math.Tan(fovRadians * 0.5f);
            float xScale = yScale / aspectRatio;
            float farMinusNear = farPlane - nearPlane;

            return new Matrix4x4(
            xScale, 0, 0, 0,
            0, yScale, 0, 0,
            0, 0, farPlane / farMinusNear, 1,
            0, 0, -nearPlane * farPlane / farMinusNear, 0
            );
        }

        private static void PlaceOffScreenIndicator(out SKPoint screenPos, Vector3 entityPosition, Vector3 cameraPosition, Vector2 cameraRotation, int screenWidth, int screenHeight, float zoomFactor)
        {
            screenPos = new SKPoint(0, 0);
            // Calculate vector from camera to entity
            Vector3 dirToEntity = entityPosition - cameraPosition;

            // Get camera's forward direction
            float yawRad = (float)cameraRotation.X.ToRadians();
            float pitchRad = (float)cameraRotation.Y.ToRadians();
            Vector3 cameraForward = new Vector3(
            (float)(Math.Cos(pitchRad) * Math.Sin(yawRad)),
            (float)Math.Sin(-pitchRad),
            (float)(Math.Cos(pitchRad) * Math.Cos(yawRad))
            );
            cameraForward = Vector3.Normalize(cameraForward);

            // Get camera's right and up vectors
            Vector3 worldUp = new Vector3(0, 1, 0);
            Vector3 cameraRight = Vector3.Normalize(Vector3.Cross(worldUp, cameraForward));
            Vector3 cameraUp = Vector3.Normalize(Vector3.Cross(cameraForward, cameraRight));

            // Project direction vector onto camera plane
            float rightComponent = Vector3.Dot(dirToEntity, cameraRight);
            float upComponent = Vector3.Dot(dirToEntity, cameraUp);
            float forwardComponent = Vector3.Dot(dirToEntity, cameraForward);

            // Calculate angle (in radians) from forward vector
            float angleHorizontal = (float)Math.Atan2(rightComponent, forwardComponent);
            float angleVertical = (float)Math.Atan2(upComponent, forwardComponent);

            // When zoomed in, we need to scale the angles to make off-screen indicators appear at the edges of the zoomed view
            // A zoom factor of 2.0 means angles are effectively doubled
            angleHorizontal *= zoomFactor;
            angleVertical *= zoomFactor;

            // Clamp angles to reasonable ranges to prevent extreme values
            float maxAngle = (float)Math.PI * 0.45f;
            angleHorizontal = Math.Clamp(angleHorizontal, -maxAngle, maxAngle);
            angleVertical = Math.Clamp(angleVertical, -maxAngle, maxAngle);

            // Normalize to screen coordinates
            float centerX = screenWidth / 2f;
            float centerY = screenHeight / 2f;
            float radius = Math.Min(centerX, centerY) * 0.85f; // 85% of half-screen

            // Calculate screen edge point using polar coordinates
            float screenEdgeX = centerX + radius * (float)Math.Sin(angleHorizontal);
            float screenEdgeY = centerY - radius * (float)Math.Sin(angleVertical);

            // Ensure the indicator stays within screen bounds with padding
            float padding = 30f;
            screenPos.X = Math.Clamp(screenEdgeX, padding, screenWidth - padding);
            screenPos.Y = Math.Clamp(screenEdgeY, padding, screenHeight - padding);
        }

        private static void ApplyDistanceBasedCorrections(ref float screenX, ref float screenY, float distance, float ndcX, float ndcY, int screenWidth, int screenHeight, float zoomFactor)
        {
            // When zoomed in, we need less correction since objects appear larger and more accurate
            float zoomCorrectionFactor = 1.0f / zoomFactor;

            // Parameters to tune correction behavior
            float maxDistanceForFullCorrection = 100f;
            float minCorrectionFactor = 0.01f * zoomCorrectionFactor;
            float maxCorrectionFactor = 0.15f * zoomCorrectionFactor;

            // Calculate distance-based scaling factor
            float distanceFactor = Math.Min(1f, maxDistanceForFullCorrection / Math.Max(1f, distance));
            float correctionStrength = minCorrectionFactor + (1f - distanceFactor) * (maxCorrectionFactor - minCorrectionFactor);

            // Apply stronger correction for distant objects near screen edges
            float edgeFactor = Math.Max(Math.Abs(ndcX), Math.Abs(ndcY));
            if (edgeFactor > 0.7f) // Objects near the edges
            {
                float edgeCorrection = (edgeFactor - 0.7f) / 0.3f; // 0 to 1 scale for edge proximity
                correctionStrength *= (1f + edgeCorrection);

                // Pull towards center based on distance and edge factor
                float centerPull = correctionStrength * edgeCorrection;
                float centerX = screenWidth / 2f;
                float centerY = screenHeight / 2f;

                screenX = screenX * (1f - centerPull) + centerX * centerPull;
                screenY = screenY * (1f - centerPull) + centerY * centerPull;
            }

            // Fine-tune vertical position based on object height relative to player
            if (distance > 50f)
            {
                // Apply additional adjustment for very distant objects
                // Less adjustment needed when zoomed in
                float verticalAdjustment = correctionStrength * 0.5f;
                screenY = screenY * (1f - verticalAdjustment) + (screenHeight / 2f) * verticalAdjustment;
            }
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
