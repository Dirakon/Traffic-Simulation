using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

internal static class PathUtils
{
    private static List<CarMovement> SplitPathByIntersections(List<CarMovement> originalPath, double reserveRadius)
    {
        return originalPath.SelectMany(carMove =>
            {
                if (carMove.GetDirection() == 0)
                    return new List<CarMovement>();
                var startingOffset = carMove.StartPosition.Offset;
                var direction = carMove.GetDirection();
                var road = carMove.GetRoad();
                var epsilon = 0.001;

                var viableIntersectionsOrderedByProximity = new List<RoadIntersection>();
                if (carMove.GetRoad().IsEnclosed())
                    viableIntersectionsOrderedByProximity = road.intersectionsWithOtherRoads
                        .Where(intersection => intersection != carMove.CorrelatingIntersection)
                        .Where(intersection => Math.Abs(intersection.GetOffsetOfRoad(road) - startingOffset) > epsilon)
                        .Select(intersection =>
                        {
                            var loopNeeded = Math.Sign(
                                                 intersection.GetOffsetOfRoad(road) -
                                                 startingOffset) !=
                                             direction;
                            return loopNeeded
                                ? (
                                    distance: road.GetMaxOffset() -
                                              Math.Abs(intersection.GetOffsetOfRoad(road) - startingOffset),
                                    intersection)
                                : (distance: Math.Abs(intersection.GetOffsetOfRoad(road) - startingOffset),
                                    intersection);
                        }).Where(tuple => tuple.distance < carMove.GetDistance())
                        .OrderBy(tuple => tuple.distance)
                        .Select(tuple => tuple.intersection)
                        .ToList();
                else
                    viableIntersectionsOrderedByProximity = road.intersectionsWithOtherRoads
                        .Where(intersection => intersection != carMove.CorrelatingIntersection)
                        .Where(intersection => Math.Abs(intersection.GetOffsetOfRoad(road) - startingOffset) > epsilon)
                        .Where(intersection => Math.Sign(
                                                   intersection.GetOffsetOfRoad(road) - startingOffset) ==
                                               direction
                        )
                        .Select(intersection =>
                            (distance: Math.Abs(intersection.GetOffsetOfRoad(road) - startingOffset), intersection)
                        )
                        .Where(tuple => tuple.distance < carMove.GetDistance())
                        .OrderBy(tuple => tuple.distance)
                        .Select(tuple => tuple.intersection)
                        .ToList();

                if (viableIntersectionsOrderedByProximity.IsEmpty()) return new List<CarMovement> {carMove};
                if (viableIntersectionsOrderedByProximity.Count == 1)
                {
                    var onlyIntersection = viableIntersectionsOrderedByProximity[0];

                    return new List<CarMovement>
                    {
                        new(carMove.StartPosition, onlyIntersection.GetPositionOfRoad(road), onlyIntersection),
                        new(onlyIntersection.GetPositionOfRoad(road), carMove.EndPosition,
                            carMove.CorrelatingIntersection)
                    };
                }

                var firstIntersection = viableIntersectionsOrderedByProximity[0];
                var traversedIntersectionPairs =
                    viableIntersectionsOrderedByProximity.Zip(viableIntersectionsOrderedByProximity.Skip(1));

                var pathsBetweenIntersectionPairs = traversedIntersectionPairs
                    .Select(intersections =>
                        new CarMovement(
                            intersections.First.GetPositionOfRoad(road),
                            intersections.Second.GetPositionOfRoad(road),
                            intersections.Second
                        )
                    ).ToList();

                var lastIntersection = pathsBetweenIntersectionPairs[^1].CorrelatingIntersection;

                return pathsBetweenIntersectionPairs
                    .Prepend(new CarMovement(carMove.StartPosition, firstIntersection.GetPositionOfRoad(road),
                        firstIntersection))
                    .Append(new CarMovement(lastIntersection.GetPositionOfRoad(road), carMove.EndPosition,
                        carMove.CorrelatingIntersection))
                    .ToList();
            }
        ).ToList();
    }

    public static List<CarMovement>? FindTheShortestPathTo(this Position startPosition, Position endPosition,
        double reserveRadius)
    {
        var path = FindShortestPath(startPosition, endPosition);
        GD.Print("PRE-READY PATH:");
        path.ForEach(el => GD.Print(el.ToString()));
        if (path != null)
            path = SplitPathByIntersections(path, reserveRadius);
        return path;
    }

    private static List<CarMovement>? FindShortestPath(Position startPosition, Position endPosition,
        double currentPathLength = 0, Dictionary<Position, double>? shortestDistances = null)
    {
        const double epsilon = 0.0001;
        if (startPosition.Road == endPosition.Road)
            return new List<CarMovement> {new(startPosition, endPosition, null)};

        shortestDistances ??= new Dictionary<Position, double>();

        if (shortestDistances.TryGetValue(startPosition, out var recordedPathLength))
        {
            if (recordedPathLength <= currentPathLength + epsilon) return null;

            shortestDistances[startPosition] = currentPathLength;
        }
        else
        {
            shortestDistances.Add(startPosition, currentPathLength);
        }

        var paths = startPosition.Road.intersectionsWithOtherRoads
            .Select(intersection =>
            {
                var currentRoad = startPosition.Road;
                var nextRoad = intersection.GetRoadOppositeFrom(currentRoad);
                var traveledDistance = Math.Abs(intersection.GetOffsetOfRoad(currentRoad) - startPosition.Offset);

                var currentEnd = new Position(intersection.GetOffsetOfRoad(currentRoad), currentRoad);
                var nextStart = new Position(intersection.GetOffsetOfRoad(nextRoad), nextRoad);
                var pathFromIntersection = FindShortestPath(nextStart, endPosition,
                    currentPathLength + traveledDistance, shortestDistances);
                (RoadIntersection intersection, Position currentEnd, List<CarMovement> pathFromIntersection, double
                    pathLength)? tuple =
                        pathFromIntersection == null
                            ? null
                            : (intersection, currentEnd, pathFromIntersection, currentPathLength + traveledDistance);
                return tuple;
            })
            .NotNull()
            .Select(tuple =>
                (tuple.pathLength, path: tuple.pathFromIntersection
                    .Prepend(new CarMovement(startPosition, tuple.currentEnd, tuple.intersection)).ToList())
            );

        var enumeratedPaths = paths.ToList();
        return enumeratedPaths.IsEmpty() ? null : enumeratedPaths.MinBy(tuple => tuple.pathLength).path;
    }
}