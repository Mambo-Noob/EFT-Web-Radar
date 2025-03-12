using AncientMountain.Managed.Services;
using AncientMountain.Managed.Skia;
using AncientMountain.Pages;
using MessagePack;
using SkiaSharp;
using System.Drawing;
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
            // Early return if object should not be drawn based on distance
            if (!ShouldDrawBasedOnDistance(localPlayer))
                return;

            // Calculate screen position and return if not visible
            if (!TryGetScreenPosition(out var scrPos, localPlayer))
                return;

            // Draw the object on screen
            DrawObjectOnScreen(canvas, localPlayer, scrPos);
        }

        // Helper method to draw the object on the screen
        private void DrawObjectOnScreen(SKCanvas canvas, WebRadarPlayer localPlayer, SKPoint scrPos)
        {
            float dist = Vector3.Distance(localPlayer.Position, Position);
            float boxHalf = 3.5f * 1f;
            string label = $"{ShortName} - {Price}";
            bool showDist = true;

            // Create the box and get paints
            SKRect boxPt = new SKRect(
                scrPos.X - boxHalf,
                scrPos.Y + boxHalf,
                scrPos.X + boxHalf,
                scrPos.Y - boxHalf
            );

            var paints = GetPaints(null);

            // Create the text point
            SKPoint textPt = new SKPoint(
                scrPos.X,
                scrPos.Y + 16f * 1f
            );

            // Draw the objects
            canvas.DrawRect(boxPt, paints.Item1);
            DrawESPText(textPt.X, textPt.Y, canvas, this, localPlayer, showDist, paints.Item2, label);
        }

        private bool TryGetScreenPosition(out SKPoint scrPos, WebRadarPlayer localPlayer)
        {
            return ScreenPositionCalculator.WorldToScreen(Position, out scrPos, localPlayer);
        }

        // Helper method to determine if the object should be drawn based on distance
        private bool ShouldDrawBasedOnDistance(WebRadarPlayer localPlayer)
        {
            float dist = Vector3.Distance(localPlayer.Position, Position);

            return true;
        }

        public void DrawESPText(float x, float y, SKCanvas canvas, WebRadarLoot entity, WebRadarPlayer localPlayer, bool printDist, SKPaint paint, params string[] lines)
        {
            var screenPos = new SKPoint(x, y + 16f * 1);
            if (printDist && lines.Length > 0)
            {
                var dist = Vector3.Distance(entity.Position, localPlayer.Position);
                var distStr = $" {dist.ToString("n1")}m";
                lines[0] += distStr;
            }
            foreach (var l in lines)
            {
                if (string.IsNullOrEmpty(l?.Trim()))
                    continue;
                canvas.DrawText(l, screenPos, paint);
                screenPos.Y += paint.TextSize;
            }
        }

        private ValueTuple<SKPaint, SKPaint> GetPaints(LootUiConfig lootConfig)
        {
            if (Price > (lootConfig == null ? 200000 : lootConfig.Important))
            {
                return new(SKPaints.PaintImportantLoot, SKPaints.TextImportantLoot);
            }
            return new(SKPaints.PaintLoot, SKPaints.TextLoot);
        }
    }
}
