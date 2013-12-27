﻿using System;
using System.Diagnostics;
using System.IO;
using BCad.Collections;
using BCad.Core;
using BCad.Entities;
using BCad.Extensions;
using BCad.FileHandlers.DrawingFiles;
using BCad.Helpers;
using BCad.Iges;
using BCad.Iges.Entities;
using BCad.Primitives;

namespace BCad.FileHandlers.Converters
{
    public class IgesConverter : IDrawingConverter
    {
        public bool ConvertToDrawing(string fileName, IDrawingFile drawingFile, out Drawing drawing, out ViewPort viewPort)
        {
            if (drawingFile == null)
                throw new ArgumentNullException("drawingFile");
            var igesFile = drawingFile as IgesDrawingFile;
            if (igesFile == null)
                throw new ArgumentException("Drawing file was not an IGES file.");
            if (igesFile.File == null)
                throw new ArgumentException("Drawing file had no internal IGES file.");
            var layer = new Layer("igs", IndexedColor.Auto);
            foreach (var entity in igesFile.File.Entities)
            {
                var cadEntity = ToEntity(entity);
                if (cadEntity != null)
                {
                    layer = layer.Add(cadEntity);
                }
            }

            drawing = new Drawing(
                new DrawingSettings(fileName, UnitFormat.Architectural, 8),
                new ReadOnlyTree<string, Layer>().Insert(layer.Name, layer),
                igesFile.File.Author);

            viewPort = drawing.ShowAllViewPort(
                Vector.ZAxis,
                Vector.YAxis,
                1016,
                491);

            return true;
        }

        public bool ConvertFromDrawing(string fileName, Drawing drawing, ViewPort viewPort, out IDrawingFile drawingFile)
        {
            var file = new IgesFile();
            file.Author = drawing.Author;
            file.FullFileName = fileName;
            file.Identification = Path.GetFileName(fileName);
            file.Identifier = Path.GetFileName(fileName);
            file.ModelUnits = ToIgesUnits(drawing.Settings.UnitFormat);
            file.ModifiedTime = DateTime.Now;
            file.SystemIdentifier = "BCad";
            file.SystemVersion = "1.0";
            foreach (var entity in drawing.GetEntities())
            {
                IgesEntity igesEntity = null;
                switch (entity.Kind)
                {
                    case EntityKind.Line:
                        igesEntity = ToIgesLine((Line)entity);
                        break;
                    default:
                        //Debug.Assert(false, "Unsupported entity type: " + entity.Kind);
                        break;
                }

                if (igesEntity != null)
                    file.Entities.Add(igesEntity);
            }

            drawingFile = new IgesDrawingFile(file);
            return true;
        }

        private static IgesLine ToIgesLine(Line line)
        {
            return new IgesLine()
            {
                Bounding = IgesBounding.BoundOnBothSides,
                Color = (IgesColorNumber)line.Color.Value,
                P1 = ToIgesPoint(line.P1),
                P2 = ToIgesPoint(line.P2)
            };
        }

        private static IgesPoint ToIgesPoint(Point point)
        {
            return new IgesPoint(point.X, point.Y, point.Z);
        }

        private static IgesUnits ToIgesUnits(UnitFormat unitFormat)
        {
            switch (unitFormat)
            {
                case UnitFormat.Architectural:
                    return IgesUnits.Inches;
                case UnitFormat.Metric:
                    return IgesUnits.Millimeters;
                default:
                    throw new Exception("Unsupported unit type: " + unitFormat);
            }
        }

        private static Entity ToEntity(IgesEntity entity)
        {
            Entity result = null;
            switch (entity.EntityType)
            {
                case IgesEntityType.CircularArc:
                    result = ToArc((IgesCircularArc)entity);
                    break;
                case IgesEntityType.Line:
                    result = ToLine((IgesLine)entity);
                    break;
                case IgesEntityType.SingularSubfigureInstance:
                    result = ToAggregate((IgesSingularSubfigureInstance)entity);
                    break;
            }

            return result;
        }

        private static Line ToLine(IgesLine line)
        {
            // TODO: handle different forms (segment, ray, continuous)
            return new Line(TransformPoint(line, line.P1), TransformPoint(line, line.P2), ToColor(line.Color));
        }

        private static Entity ToArc(IgesCircularArc arc)
        {
            var center = TransformPoint(arc, arc.Center);
            var startPoint = TransformPoint(arc, arc.StartPoint);
            var endPoint = TransformPoint(arc, arc.EndPoint);

            // generate normal; points are given CCW
            var startVector = startPoint - center;
            var endVector = endPoint - center;
            Vector normal;
            if (((Point)startVector).CloseTo(endVector))
                normal = Vector.NormalFromRightVector(startVector.Normalize());
            else
                normal = startVector.Cross(endVector).Normalize();
            Debug.Assert(startVector.IsOrthoganalTo(normal));
            Debug.Assert(endVector.IsOrthoganalTo(normal));

            // find radius from start/end points
            var startRadius = startVector.Length;
            var endRadius = endVector.Length;
            Debug.Assert(MathHelper.CloseTo(startRadius, endRadius));
            var radius = startRadius;

            // if start/end points are the same, it's a circle.  otherwise it's an arc
            if (startPoint.CloseTo(endPoint))
            {
                return new Circle(center, radius, normal, ToColor(arc.Color));
            }
            else
            {
                // project back to unit circle to find start/end angles
                var primitiveCircle = new PrimitiveEllipse(center, radius, normal);
                var toUnit = primitiveCircle.FromUnitCircleProjection();
                toUnit.Invert();
                var startUnit = toUnit.Transform(startPoint);
                var endUnit = toUnit.Transform(endPoint);
                var startAngle = ((Vector)startUnit).ToAngle();
                var endAngle = ((Vector)endUnit).ToAngle();
                return new Arc(center, radius, startAngle, endAngle, normal, ToColor(arc.Color));
            }
        }

        private static Entity ToAggregate(IgesSingularSubfigureInstance subfigure)
        {
            var sub = subfigure.Subfigure;
            if (sub != null)
            {
                var ag = sub as IgesSubfigureDefinition;
                if (ag != null)
                {
                    var entities = ReadOnlyList<Entity>.Empty();
                    foreach (var e in ag.Entities)
                    {
                        var a = ToEntity(e);
                        if (a != null)
                            entities = entities.Add(a);
                    }

                    if (entities.Count != 0)
                    {
                        var offset = new Point(subfigure.Offset.X, subfigure.Offset.Y, subfigure.Offset.Z);
                        return new AggregateEntity(offset, entities, ToColor(subfigure.Color));
                    }
                }
            }

            return null;
        }

        private static IndexedColor ToColor(IgesColorNumber color)
        {
            switch (color)
            {
                case IgesColorNumber.Default:
                    return IndexedColor.Auto;
                case IgesColorNumber.Black:
                    return new IndexedColor(0);
                case IgesColorNumber.Red:
                    return new IndexedColor(1);
                case IgesColorNumber.Green:
                    return new IndexedColor(3);
                case IgesColorNumber.Blue:
                    return new IndexedColor(5);
                case IgesColorNumber.Yellow:
                    return new IndexedColor(2);
                case IgesColorNumber.Magenta:
                    return new IndexedColor(6);
                case IgesColorNumber.Cyan:
                    return new IndexedColor(4);
                case IgesColorNumber.White:
                    return new IndexedColor(7);
                default:
                    return IndexedColor.Auto;
            }
        }

        private static Point TransformPoint(IgesEntity entity, IgesPoint point)
        {
            var transformed = entity.TransformationMatrix.Transform(point);
            return new Point(transformed.X, transformed.Y, transformed.Z);
        }
    }
}
