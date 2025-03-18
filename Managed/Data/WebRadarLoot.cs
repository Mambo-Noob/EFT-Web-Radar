using AncientMountain.Managed.Services;
using AncientMountain.Managed.Skia;
using MessagePack;
using SkiaSharp;
using System.Numerics;

namespace AncientMountain.Managed.Data
{
    [MessagePackObject]
    public readonly struct WebRadarLoot : IEntity
    {
        [IgnoreMember]
        public readonly string Id => $"{ShortName}-{Position.ToString()}";
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
            } else if (heightDiff < -1.45) // loot is below player
            {
                using var path = point.GetDownArrow(5);
                canvas.DrawPath(path, SKPaints.ShapeOutline);
                canvas.DrawPath(path, paints.Item1);
            } else // loot is level with player
            {
                var size = 5 * RadarService.Scale;
                canvas.DrawCircle(point, size, SKPaints.ShapeOutline);
                canvas.DrawCircle(point, size, paints.Item1);
            }

            point.Offset(7 * RadarService.Scale, 3 * RadarService.Scale);
            canvas.DrawText(label, point, SKPaints.TextOutline); // Draw outline
            canvas.DrawText(label, point, paints.Item2);
        }

        public void DrawESP(SKCanvas canvas, WebRadarPlayer localPlayer, LootUiConfig lootUiConfig, ESPUiConfig espConfig)
        {
            var distance = Vector3.Distance(localPlayer.Position, Position);
            if (distance > 200)
            {
                return;
            }
            if (!ScreenPositionCalculator.WorldToScreenPositionOnEnemyView(out var point, this, localPlayer, espConfig.ScreenWidth, espConfig.ScreenHeight, espConfig.FOV))
            {
                return;
            }

            var paints = GetPaints(lootUiConfig);

            canvas.DrawCircle(point, RadarService.Scale, paints.Item1);
            //Make this more like local radar
            canvas.DrawText($"{ShortName} - {Price} - {distance.ToString("n2")}m", point, paints.Item2);
        }

        private ValueTuple<SKPaint, SKPaint> GetPaints(LootUiConfig lootConfig)
        {
            if (Price > (lootConfig == null ? 200000 : lootConfig.Important))
            {
                return new(SKPaints.PaintImportantLoot, SKPaints.TextImportantLoot);
            }
            //Add more to this
            return new(SKPaints.PaintLoot, SKPaints.TextLoot);
        }
    }
}
