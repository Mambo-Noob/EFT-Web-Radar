﻿using AncientMountain.Managed.Data;
using AncientMountain.Managed.Services;
using SkiaSharp;

namespace AncientMountain.Managed.Skia
{
    public static class CanvasHelper
    {
        public static void CanvasDrawIndicator(this SKCanvas canvas, SKPaint paint, float heightDiff, SKPoint point, float size)
        {
            if (heightDiff > 1.45) // loot is above player
            {
                using var path = point.GetUpArrow(5);
                canvas.DrawPath(path, SKPaints.ShapeOutline);
                canvas.DrawPath(path, paint);
            } else if (heightDiff < -1.45) // loot is below player
            {
                using var path = point.GetDownArrow(5);
                canvas.DrawPath(path, SKPaints.ShapeOutline);
                canvas.DrawPath(path, paint);
            } else // loot is level with player
            {
                canvas.DrawCircle(point, size, SKPaints.ShapeOutline);
                canvas.DrawCircle(point, size, paint);
            }
        }

        public static void DrawLineToPOI(this SKCanvas canvas, IEntity startingEntity, RadarService.MapParameters mapParams, SKPoint interestPoint)
        {
            var startPoint = startingEntity.Position.ToMapPos(mapParams.Map).ToZoomedPos(mapParams);
            canvas.DrawLine(startPoint, interestPoint, SKPaints.POILine);
        }
    }
}
