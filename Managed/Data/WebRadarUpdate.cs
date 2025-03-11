using AncientMountain.Managed.Services;
using AncientMountain.Managed.Skia;
using AncientMountain.Pages;
using MessagePack;
using SkiaSharp;
using System.Numerics;

namespace AncientMountain.Managed.Data
{
    [MessagePackObject]
    public sealed class WebRadarUpdate
    {
        /// <summary>
        /// Update version (used for ordering).
        /// </summary>
        [Key(0)]
        public uint Version { get; set; } = 0;
        /// <summary>
        /// True if In-Game, otherwise False.
        /// </summary>
        [Key(1)]
        public bool InGame { get; set; } = false;
        /// <summary>
        /// Contains the Map ID of the current map.
        /// </summary>
        [Key(2)]
        public string MapID { get; set; } = null;
        /// <summary>
        /// All Players currently on the map.
        /// </summary>
        [Key(3)]
        public IEnumerable<WebRadarPlayer> Players { get; set; } = null;

        [Key(4)]
        public IEnumerable<WebRadarLoot> Loot { get; set; } // NEW: Loot Data
    }

    [MessagePackObject]
    public readonly struct WebRadarLoot
    {
        /// <summary>
        /// Item's Short Name.
        /// </summary>
        [Key(0)]
        public readonly string ShortName { get; init; }

        /// <summary>
        /// Item's Price (Roubles).
        /// </summary>
        [Key(1)]
        public readonly int Price { get; init; }

        /// <summary>
        /// Item's Position.
        /// </summary>
        [Key(2)]
        public readonly Vector3 Position { get; init; }

        /// <summary>
        /// Item is Meds.
        /// </summary>
        [Key(3)]
        public bool IsMeds { get; init; }

        /// <summary>
        /// Item is Food.
        /// </summary>
        [Key(4)]
        public bool IsFood { get; init; }

        /// <summary>
        /// Item is Backpack.
        /// </summary>
        [Key(5)]
        public bool IsBackpack { get; init; }

        public void Draw(SKCanvas canvas, SKImageInfo info, RadarService.MapParameters mapParams, WebRadarPlayer localPlayer, LootUiConfig lootConfig)
        {
            var label = $"{ShortName} - ₽{Price}";
            var paints = GetPaints(lootConfig);
            var heightDiff = Position.Y - localPlayer.Position.Y;
            var point = Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams);
            //MouseoverPosition = new Vector2(point.X, point.Y);
            SKPaints.ShapeOutline.StrokeWidth = 2f;
            if (heightDiff > 1.45) // loot is above player
            {
                using var path = point.GetUpArrow(5);
                canvas.DrawPath(path, SKPaints.ShapeOutline);
                canvas.DrawPath(path, paints.Item1);
            }
            else if (heightDiff < -1.45) // loot is below player
            {
                using var path = point.GetDownArrow(5);
                canvas.DrawPath(path, SKPaints.ShapeOutline);
                canvas.DrawPath(path, paints.Item1);
            }
            else // loot is level with player
            {
                var size = 5 * RadarService.Scale;
                canvas.DrawCircle(point, size, SKPaints.ShapeOutline);
                canvas.DrawCircle(point, size, paints.Item1);
            }

            point.Offset(7 * RadarService.Scale, 3 * RadarService.Scale);
            canvas.DrawText(label, point, SKPaints.TextOutline); // Draw outline
            canvas.DrawText(label, point, paints.Item2);
        }

        public void DrawESP(SKCanvas canvas, WebRadarPlayer localPlayer)
        {
            var dist = Vector3.Distance(localPlayer.Position, Position);

            //Get local player screen rotation

            
            var boxHalf = 3.5f * ESP.Config.FontScale;
            var label = GetUILabel(MainForm.Config.QuestHelper.Enabled);
            var showDist = ESP.Config.ShowDistances || dist <= 10f;
            var boxPt = new SKRect(scrPos.X - boxHalf, scrPos.Y + boxHalf,
                scrPos.X + boxHalf, scrPos.Y - boxHalf);
            var paints = GetESPPaints();
            var textPt = new SKPoint(scrPos.X,
                scrPos.Y + 16f * ESP.Config.FontScale);
            canvas.DrawRect(boxPt, paints.Item1);
            textPt.DrawESPText(canvas, this, localPlayer, showDist, paints.Item2, label);
        }

        public Vector2 GetScreenPosition(
            Vector3 player1Position,
            Vector3 player1Forward,
            Vector3 player1Up,
            Vector3 player2Position,
            float fieldOfViewHorizontal,
            float aspectRatio)
        {
            // Create the view coordinate system for player 1
            Vector3 forward = Vector3.Normalize(player1Forward);
            Vector3 right = Vector3.Normalize(Vector3.Cross(player1Up, forward));
            Vector3 up = Vector3.Cross(forward, right);

            // Vector from player 1 to player 2
            Vector3 toPlayer2 = player2Position - player1Position;

            // Project this vector onto the view plane
            float forwardDistance = Vector3.Dot(toPlayer2, forward);

            // If the player is behind us, return an off-screen position
            if (forwardDistance <= 0)
            {
                return new Vector2(-2, -2); // Off-screen indicator
            }

            // Project onto the right and up vectors to get the position in view space
            float rightAmount = Vector3.Dot(toPlayer2, right);
            float upAmount = Vector3.Dot(toPlayer2, up);

            // Convert to normalized device coordinates (-1 to 1 range)
            // Use the horizontal FOV and aspect ratio to calculate
            float halfFovRadians = (fieldOfViewHorizontal * 0.5f) * (float)(Math.PI / 180f);
            float tanHalfFov = (float)Math.Tan(halfFovRadians);

            float ndcX = rightAmount / (forwardDistance * tanHalfFov);
            float ndcY = upAmount / (forwardDistance * tanHalfFov / aspectRatio);

            // Return the normalized screen coordinates (-1 to 1 range)
            // Where (0,0) is screen center, (-1,-1) is bottom-left, (1,1) is top-right
            return new Vector2(ndcX, ndcY);
        }

        // Extension method to check if the position is on screen
        public bool IsOnScreen(Vector2 screenPosition)
        {
            return screenPosition.X >= -1 && screenPosition.X <= 1 &&
                   screenPosition.Y >= -1 && screenPosition.Y <= 1;
        }

        // Optionally, convert to pixel coordinates
        public Vector2 NormalizedToPixelCoordinates(Vector2 normalizedPosition, int screenWidth, int screenHeight)
        {
            float pixelX = (normalizedPosition.X + 1) * 0.5f * screenWidth;
            float pixelY = (1 - (normalizedPosition.Y + 1) * 0.5f) * screenHeight; // Y is flipped in most screen spaces

            return new Vector2(pixelX, pixelY);
        }

        private ValueTuple<SKPaint, SKPaint> GetPaints(LootUiConfig lootConfig)
        {
            if (Price > lootConfig.Important)
            {
                return new(SKPaints.PaintImportantLoot, SKPaints.TextImportantLoot);
            }
            return new(SKPaints.PaintLoot, SKPaints.TextLoot);
        }
    }
}
