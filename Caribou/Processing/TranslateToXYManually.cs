namespace Caribou.Processing
{
    using System;
    using System.Collections.Generic;
    using Caribou.Models;
    using Rhino;
    using Rhino.Geometry;

    public static class TranslateToXYManually
    {
        // The largest 'gap' size that will attempted to be automatically closed to try and make closed curves for buildings
        static double ALLOWABLE_CLOSURE = 1.0 * RhinoMath.UnitScale(UnitSystem.Meters, RhinoDoc.ActiveDoc.ModelUnitSystem);
            
        public static Dictionary<OSMTag, List<Point3d>> NodePointsFromCoords(RequestHandler result)
        {
            var geometryResult = new Dictionary<OSMTag, List<Point3d>>();
            var unitScale = RhinoMath.UnitScale(UnitSystem.Meters, RhinoDoc.ActiveDoc.ModelUnitSystem); // OSM conversion assumes meters
            Coord lengthPerDegree = GetDegreesPerAxis(result.MinBounds, result.MaxBounds, unitScale);

            foreach (var entry in result.FoundData)
            {
                geometryResult[entry.Key] = new List<Point3d>();
                foreach (FoundItem item in entry.Value)
                {
                    var pt = GetPointFromLatLong(item.Coords[0], lengthPerDegree, result.MinBounds);
                    geometryResult[entry.Key].Add(pt);
                }
            }

            return geometryResult;
        }

        public static Dictionary<OSMTag, List<PolylineCurve>> WayPolylinesFromCoords(RequestHandler result)
        {
            var geometryResult = new Dictionary<OSMTag, List<PolylineCurve>>();
            var unitScale = RhinoMath.UnitScale(UnitSystem.Meters, RhinoDoc.ActiveDoc.ModelUnitSystem); // OSM conversion assumes meters
            Coord lengthPerDegree = GetDegreesPerAxis(result.MinBounds, result.MaxBounds, unitScale);

            foreach (var entry in result.FoundData)
            {
                geometryResult[entry.Key] = new List<PolylineCurve>();
                foreach (FoundItem item in entry.Value)
                {
                    var linePoints = new List<Point3d>();
                    foreach (var coord in item.Coords)
                    {
                        var pt = GetPointFromLatLong(coord, lengthPerDegree, result.MinBounds);
                        linePoints.Add(pt);
                    }

                    var polyLine = new PolylineCurve(linePoints); // Creating a polylinecurve from scratch makes invalid geometry
                    geometryResult[entry.Key].Add(polyLine);
                }
            }

            return geometryResult;
        }

        public static Dictionary<OSMTag, List<Brep>> BuildingBrepsFromCoords(ref RequestHandler result, bool outputHeighted)
        {
            var geometryResult = new Dictionary<OSMTag, List<Brep>>();
            var unitScale = RhinoMath.UnitScale(UnitSystem.Meters, RhinoDoc.ActiveDoc.ModelUnitSystem); // OSM conversion assumes meters
            var tolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance; 
            Coord lengthPerDegree = GetDegreesPerAxis(result.MinBounds, result.MaxBounds, unitScale);

            foreach (var entry in result.FoundData)
            {
                geometryResult[entry.Key] = new List<Brep>();

                for (int i = entry.Value.Count - 1; i >= 0; i--)
                {
                    var outlinePoints = new List<Point3d>();
                    foreach (var coord in entry.Value[i].Coords)
                        outlinePoints.Add(GetPointFromLatLong(coord, lengthPerDegree, result.MinBounds));

                    var outline = new PolylineCurve(outlinePoints); // Creating a polylinecurve from scratch makes invalid geometry
                    if (!outline.IsClosed)
                    {
                        if (outline.IsClosable(ALLOWABLE_CLOSURE)) // Force-close the curve
                        {
                            outline.MakeClosed(ALLOWABLE_CLOSURE); 
                        }
                        else // Skip this curve as no valid Brep can be made
                        {
                            entry.Value.RemoveAt(i); 
                            continue;
                        }
                    }

                    var height = GetBuildingHeights.ParseHeight(entry.Value[i].Tags, unitScale);
                    if (outputHeighted && height > 0.0) // Output heighted buildings
                    {
                        var toHeight = new Vector3d(0, 0, height);

                        var envelope = Surface.CreateExtrusion(outline, toHeight);
                        var floor = Brep.CreatePlanarBreps(outline, tolerance);
                        outline.Translate(toHeight);
                        var roof = Brep.CreatePlanarBreps(outline, tolerance);

                        var volume = Brep.JoinBreps(new Brep[] { floor[0], envelope.ToBrep(), roof[0] }, tolerance);
                        geometryResult[entry.Key].Add(volume[0]);
                    }
                    else if (!outputHeighted && height == 0.0) // Output unheighted buildings
                    {
                        var builtSurface = Brep.CreatePlanarBreps(outline, tolerance);
                        if (builtSurface != null && builtSurface.Length > 0)
                            geometryResult[entry.Key].Add(builtSurface[0]);
                    }
                    else // Item wasn't matched, so should be removed from result so its metadata is not output
                        entry.Value.RemoveAt(i);
                }

                geometryResult[entry.Key].Reverse(); // We iterated in reverse order, so swap list back to right direction
            }

            return geometryResult;
        }

        public static Dictionary<OSMTag, List<Brep>> RelationBrepsFromCoords(RequestHandler result)
        {
            var geometryResult = new Dictionary<OSMTag, List<Brep>>();
            var unitScale = RhinoMath.UnitScale(UnitSystem.Meters, RhinoDoc.ActiveDoc.ModelUnitSystem);
            var tolerance = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;
            Coord lengthPerDegree = GetDegreesPerAxis(result.MinBounds, result.MaxBounds, unitScale);

            // Diagnostic counters
            int totalRelationsProcessed = 0;
            int totalOuterCurvesCreated = 0;
            int totalInnerCurvesCreated = 0;
            int totalBrepsCreated = 0;
            int failedClosures = 0;
            int failedBrepCreations = 0;

            foreach (var entry in result.FoundData)
            {
                geometryResult[entry.Key] = new List<Brep>();

                foreach (FoundItem item in entry.Value)
                {
                    // Only process relations (items with Members)
                    if (item.Members == null || item.Members.Count == 0)
                        continue;

                    totalRelationsProcessed++;

                    // Separate members by role
                    var outerCurves = new List<PolylineCurve>();
                    var innerCurves = new List<PolylineCurve>();

                    foreach (var member in item.Members)
                    {
                        var memberPoints = new List<Point3d>();
                        foreach (var coord in member.Coords)
                        {
                            var pt = GetPointFromLatLong(coord, lengthPerDegree, result.MinBounds);
                            memberPoints.Add(pt);
                        }

                        if (memberPoints.Count > 0)
                        {
                            var curve = new PolylineCurve(memberPoints);

                            // Force closure if within tolerance
                            if (!curve.IsClosed && curve.IsClosable(ALLOWABLE_CLOSURE))
                            {
                                curve.MakeClosed(ALLOWABLE_CLOSURE);
                            }

                            // Categorize by role (empty role defaults to outer)
                            if (member.Role == "inner")
                            {
                                innerCurves.Add(curve);
                                totalInnerCurvesCreated++;
                            }
                            else // "outer" or empty
                            {
                                outerCurves.Add(curve);
                                totalOuterCurvesCreated++;
                            }
                        }
                    }

                    // Create Breps from outer curves
                    if (outerCurves.Count > 0)
                    {
                        foreach (var outerCurve in outerCurves)
                        {
                            if (!outerCurve.IsClosed)
                            {
                                failedClosures++;
                                continue; // Skip non-closed curves
                            }

                            var outerBreps = Brep.CreatePlanarBreps(outerCurve, tolerance);
                            if (outerBreps == null || outerBreps.Length == 0)
                            {
                                failedBrepCreations++;
                                continue;
                            }

                            var resultBrep = outerBreps[0];

                            // Subtract inner curves (holes) from this outer Brep
                            if (innerCurves.Count > 0)
                            {
                                foreach (var innerCurve in innerCurves)
                                {
                                    if (!innerCurve.IsClosed)
                                        continue;

                                    var innerBreps = Brep.CreatePlanarBreps(innerCurve, tolerance);
                                    if (innerBreps != null && innerBreps.Length > 0)
                                    {
                                        var difference = Brep.CreateBooleanDifference(resultBrep, innerBreps[0], tolerance);
                                        if (difference != null && difference.Length > 0)
                                        {
                                            resultBrep = difference[0];
                                        }
                                        // If boolean fails, keep the outer Brep without the hole
                                    }
                                }
                            }

                            geometryResult[entry.Key].Add(resultBrep);
                            totalBrepsCreated++;
                        }
                    }
                }
            }

            // Add diagnostic output to RelationParsingStats
            var diagnosticLine = $"Geometry: Processed {totalRelationsProcessed} relations, created {totalOuterCurvesCreated} outer curves, {totalInnerCurvesCreated} inner curves, {totalBrepsCreated} Breps";
            if (failedClosures > 0 || failedBrepCreations > 0)
            {
                diagnosticLine += $" (failed: {failedClosures} closures, {failedBrepCreations} Brep creations)";
            }

            // Append to existing stats
            if (!string.IsNullOrEmpty(result.RelationParsingStats))
            {
                result.RelationParsingStats += "\n" + diagnosticLine;
            }
            else
            {
                result.RelationParsingStats = diagnosticLine;
            }

            return geometryResult;
        }

        public static Dictionary<OSMTag, List<PolylineCurve>> RelationPolylinesFromCoords(RequestHandler result)
        {
            var geometryResult = new Dictionary<OSMTag, List<PolylineCurve>>();
            var unitScale = RhinoMath.UnitScale(UnitSystem.Meters, RhinoDoc.ActiveDoc.ModelUnitSystem);
            Coord lengthPerDegree = GetDegreesPerAxis(result.MinBounds, result.MaxBounds, unitScale);

            foreach (var entry in result.FoundData)
            {
                geometryResult[entry.Key] = new List<PolylineCurve>();

                foreach (FoundItem item in entry.Value)
                {
                    // Only process relations (items with Members)
                    if (item.Members == null || item.Members.Count == 0)
                        continue;

                    // Create a polyline for each member (outer and inner rings)
                    foreach (var member in item.Members)
                    {
                        var memberPoints = new List<Point3d>();
                        foreach (var coord in member.Coords)
                        {
                            var pt = GetPointFromLatLong(coord, lengthPerDegree, result.MinBounds);
                            memberPoints.Add(pt);
                        }

                        if (memberPoints.Count > 0)
                        {
                            var curve = new PolylineCurve(memberPoints);

                            // Force closure if within tolerance
                            if (!curve.IsClosed && curve.IsClosable(ALLOWABLE_CLOSURE))
                            {
                                curve.MakeClosed(ALLOWABLE_CLOSURE);
                            }

                            geometryResult[entry.Key].Add(curve);
                        }
                    }
                }
            }

            return geometryResult;
        }

        // Thanks to Elk for this code!
        public static Coord GetDegreesPerAxis(Coord min, Coord max, double unitScale)
        {
            double meanEarthRadius = 6371000;
            // Get the median latitude value for evaluating the earth's radius at the location
            double averageLat = min.Latitude + ((max.Latitude - min.Latitude) / 2);
            double yLenPerDegree = ((Math.PI * meanEarthRadius) / 180) * unitScale;
            double xLenPerDegree = ((Math.PI * (Math.Cos((averageLat * Math.PI) / 180) * 6371000)) / 180) * unitScale;
            return new Coord(xLenPerDegree, yLenPerDegree);
        }

        public static Point3d GetPointFromLatLong(Coord ptCoord, Coord degreesPerCoord, Coord minExtends)
        {
            var x = (ptCoord.Longitude - minExtends.Longitude) * degreesPerCoord.Latitude;
            var y = (ptCoord.Latitude - minExtends.Latitude) * degreesPerCoord.Longitude;
            return new Point3d(x, y, 0);
        }
    }
}
