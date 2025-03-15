using AncientMountain.Managed.Data;
using SkiaSharp;
using System.Drawing;
using System.Numerics;

namespace AncientMountain.Managed.Services
{
    public static class ScreenPositionCalculator
    {
        private const int VIEWPORT_TOLERANCE = 800;

        public static bool WorldToScreenWithPlayer(Vector3 worldPos, out SKPoint scrPos, WebRadarPlayer player, bool onScreenCheck = false, bool useTolerance = false)
        {
            var Viewport = new Rectangle(0, 0, 1920, 1080);
            var ViewportCenter = new SKPoint(Viewport.Width / 2f, Viewport.Height / 2f);
            scrPos = default;

            // Calculate view vectors based on player rotation
            float pitch = player.Rotation.Y * (MathF.PI / 180f);  // Convert to radians
            float yaw = player.Rotation.X * (MathF.PI / 180f);    // Convert to radians

            // Forward vector (direction the player is facing)
            Vector3 forward = new Vector3(
                MathF.Cos(pitch) * MathF.Cos(yaw),
                MathF.Cos(pitch) * MathF.Sin(yaw),
                MathF.Sin(pitch)
            );

            // Right vector (perpendicular to forward, pointing right)
            Vector3 right = new Vector3(
                MathF.Sin(yaw - MathF.PI / 2),
                MathF.Cos(yaw - MathF.PI / 2),
                0
            );

            // Up vector (perpendicular to forward and right)
            Vector3 up = Vector3.Cross(right, forward);

            // Calculate relative position from player to world position
            Vector3 relativePos = worldPos - player.Position;

            // Check if object is in front of the player (dot product with forward vector is positive)
            // This ensures we only draw objects that are in the player's field of view
            float dotForward = Vector3.Dot(forward, relativePos);
            if (dotForward <= 0)
            {
                // Object is behind the player
                return false;
            }

            // FOV check (normalize the angle between forward and the object vector)
            // This ensures that objects outside the player's FOV aren't drawn
            float fov = 90.0f; // Default FOV - replace with player.FOV if available
            float relativeDistance = relativePos.Length();
            if (relativeDistance > 0.001f) // Avoid division by zero
            {
                Vector3 directionToObject = relativePos / relativeDistance;
                float angleCos = Vector3.Dot(forward, directionToObject);
                float angleRadians = MathF.Acos(angleCos);
                float angleDegrees = angleRadians * (180.0f / MathF.PI);

                if (angleDegrees > fov / 2)
                {
                    // Object is outside the player's FOV
                    return false;
                }
            }

            // W component acts as a depth value
            float w = dotForward;

            if (w < 0.098f)
            {
                // Too close to the near plane
                return false;
            }

            // Calculate screen coordinates using negative dot products for proper orientation
            float x = -Vector3.Dot(right, relativePos) / w;
            float y = -Vector3.Dot(up, relativePos) / w;

            // Handle scoped adjustments (if player has these properties)
            float aspect = 16.0f / 9.0f; // Default aspect ratio - replace with player.AspectRatio if available
            bool isScoped = false; // Default - replace with player.IsScoped if available

            if (isScoped)
            {
                float angleRadHalf = (MathF.PI / 180f) * fov * 0.5f;
                float angleCtg = MathF.Cos(angleRadHalf) / MathF.Sin(angleRadHalf);

                x /= angleCtg * aspect * 0.5f;
                y /= angleCtg * 0.5f;
            }

            // Calculate screen position - apply FOV scaling
            float fovScale = MathF.Tan((fov * 0.5f) * (MathF.PI / 180.0f));
            x = x / fovScale / aspect;
            y = y / fovScale;

            var center = ViewportCenter; // Use existing ViewportCenter
            scrPos = new SKPoint
            {
                X = center.X * (1.0f - x), // Invert X for correct left/right orientation
                Y = center.Y * (1.0f - y)  // Invert Y for correct up/down orientation
            };

            // Optional on-screen check
            if (onScreenCheck)
            {
                int left = useTolerance ? Viewport.Left - VIEWPORT_TOLERANCE : Viewport.Left;
                int rightl = useTolerance ? Viewport.Right + VIEWPORT_TOLERANCE : Viewport.Right;
                int top = useTolerance ? Viewport.Top - VIEWPORT_TOLERANCE : Viewport.Top;
                int bottom = useTolerance ? Viewport.Bottom + VIEWPORT_TOLERANCE : Viewport.Bottom;

                if (scrPos.X < left || scrPos.X > rightl || scrPos.Y < top || scrPos.Y > bottom)
                {
                    scrPos = default;
                    return false;
                }
            }

            return true;
        }

        public static bool IsFacingTarget(WebRadarPlayer lPlayer, WebRadarPlayer enemy, float? maxDist = null)
        {
            var distance = Vector3.Distance(lPlayer.Position, enemy.Position);
            if (maxDist is float maxDistFloat && distance > maxDistFloat)
                return false;

            // Calculate the 3D vector from enemy to player (including vertical component)
            Vector3 directionToTarget = Vector3.Normalize(enemy.Position - lPlayer.Position);

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
        public static bool WorldToScreenPositionOnEnemyView(out SKPoint screenPos, WebRadarPlayer enemy, WebRadarPlayer lPlayer, int screenWidth = 2560, int screenHeight = 1440, float horizontalFOV = 70f)
        {
            screenPos = new SKPoint(0, 0);

            // First check if the lPlayer is actually facing the enemy
            // We can reuse the existing function for this initial check
            if (!IsFacingTarget(lPlayer, enemy))
                return false;

            // Calculate the vector from player to enemy
            Vector3 directionToEnemy = Vector3.Normalize(enemy.Position - lPlayer.Position);

            // Get the player's forward direction
            Vector3 lPlayerForward = Vector3.Normalize(RotationToDirection(lPlayer.Rotation));

            // Calculate right and up vectors for the player's view
            Vector3 lPlayerRight = -Vector3.Normalize(Vector3.Cross(lPlayerForward, new Vector3(0, 1, 0)));
            Vector3 lPlayerUp = Vector3.Normalize(Vector3.Cross(lPlayerRight, lPlayerForward));

            // Convert horizontal FOV to radians
            float hFovRad = horizontalFOV * (float)Math.PI / 180f;

            // Calculate vertical FOV based on aspect ratio
            float aspectRatio = (float)screenWidth / screenHeight;
            float vFovRad = 2 * (float)Math.Atan(Math.Tan(hFovRad / 2) / aspectRatio);

            // Calculate dot products to determine the position in view space
            float dotForward = Vector3.Dot(directionToEnemy, lPlayerForward);

            // If dot product with forward is negative or near zero, the target is behind or to the side
            if (dotForward <= 0.01f)
                return false;

            // Calculate normalized screen coordinates (-1 to 1)
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
            screenPos.X = (-ndcX + 1) * screenWidth / 2;
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
