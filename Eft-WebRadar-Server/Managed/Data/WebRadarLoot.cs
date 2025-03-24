using AncientMountain.Managed.Services;
using AncientMountain.Managed.Skia;
using MessagePack;
using SkiaSharp;
using System.Numerics;

namespace AncientMountain.Managed.Data
{
    [MessagePackObject]
    public sealed class WebRadarLoot
    {
        [Key(0)]
        public string ShortName { get; init; }

        [Key(1)]
        public int Price { get; init; }

        [Key(2)]
        public Vector3 Position { get; init; }

        [Key(3)]
        public bool IsMeds { get; init; }

        [Key(4)]
        public bool IsFood { get; init; }

        [Key(5)]
        public bool IsBackpack { get; init; }

        public void Draw(SKCanvas canvas, SKImageInfo info, RadarService.MapParameters mapParams, WebRadarPlayer localPlayer, LootFilterService lootFilter)
        {
            try
            {
                // 🚨 Ignore loot if filters are disabled
                if ((!lootFilter.ShowFood && IsFood) || 
                    (!lootFilter.ShowMeds && IsMeds) || 
                    (!lootFilter.ShowBackpacks && IsBackpack))
                {
                    return; // Do not render this loot
                }

                var point = Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams);
                if (point.X < info.Rect.Left - 15 || point.X > info.Rect.Right + 15 ||
                    point.Y < info.Rect.Top - 15 || point.Y > info.Rect.Bottom + 15)
                    return; // Loot is outside of the map bounds

                var AdjustedPrice = Price / 1000;

                // ✅ Fix backpack naming issue
                var Name = ShortName.StartsWith("NULL") ? "Backpack" : ShortName;

                var label = $"{Name} [{AdjustedPrice}K]";

                DrawLootMarker(canvas, point, Price, lootFilter);
                DrawLootText(canvas, point, label);
            }
            catch
            {
                // Handle error
            }
        }


        private void DrawLootMarker(SKCanvas canvas, SKPoint point, int price, LootFilterService lootFilter)
        {
            var size = 4 * RadarService.Scale;
            canvas.DrawCircle(point, size, SKPaints.ShapeOutline);
        
            // 🚨 Stop drawing if filter is off
            if (!lootFilter.ShowBackpacks && IsBackpack) return;
        
            // 🟢 Draw Food, Meds, and Backpacks (if enabled)
            if (lootFilter.ShowFood && IsFood || lootFilter.ShowMeds && IsMeds || lootFilter.ShowBackpacks && IsBackpack)
            {
                canvas.DrawCircle(point, size, SKPaints.PaintLootFMB);
                return;
            }
        
            // 🔴 Highlight Important Loot
            if (lootFilter.IsImportantLoot(this))
            {
                canvas.DrawCircle(point, size, SKPaints.PaintLootImportant);
            }
            else
            {
                canvas.DrawCircle(point, size, SKPaints.PaintLoot);
            }
        }



        private void DrawLootText(SKCanvas canvas, SKPoint point, string label)
        {
            point.Offset(9 * RadarService.Scale, 3 * RadarService.Scale);
            canvas.DrawText(label, point, SKPaints.TextOutline);
            canvas.DrawText(label, point, SKPaints.TextLoot);
        }
    }
}
