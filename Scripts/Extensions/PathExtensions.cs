using System.Collections.Generic;
using System.Linq;

namespace TrafficSimulation.scripts.extensions;

internal static class PathExtensions
{
    private static List<CarMovement> SplitPathByIntersections(List<CarMovement> originalPath, double reserveRadius)
    {
        return originalPath.SelectMany(carMove =>
            {
                if (carMove.Direction == 0)
                    return new List<CarMovement>();
                var startingOffset = carMove.StartPosition.Offset;
                var direction = carMove.Direction;
                var road = carMove.GetRoad();

                var viableIntersectionsOrderedByProximity = road.IntersectionsWithOtherRoads
                    .Where(intersection => intersection != carMove.CorrelatingIntersection)
                    .Where(intersection =>
                        !road.GetShortestPath(intersection.GetOffsetOfRoad(road), startingOffset).Distance
                            .AlmostEqualTo(0))
                    .Select(intersection =>
                    {
                        var singleRoadPath =
                            road.GetPathWithSetDirection(startingOffset, intersection.GetOffsetOfRoad(road), direction);

                        return singleRoadPath == null
                            ? null
                            : (
                                distance: singleRoadPath.Distance,
                                intersection).ToNullable();
                    })
                    .NotNull()
                    .Where(tuple => tuple.distance < carMove.Distance)
                    .OrderBy(tuple => tuple.distance)
                    .Select(tuple => tuple.intersection)
                    .ToList();

                if (viableIntersectionsOrderedByProximity.IsEmpty()) return new List<CarMovement> {carMove};
                if (viableIntersectionsOrderedByProximity.Count == 1)
                {
                    var onlyIntersection = viableIntersectionsOrderedByProximity[0];

                    return new List<CarMovement>
                    {
                        new(carMove.StartPosition, onlyIntersection.GetPositionOnRoad(road), onlyIntersection),
                        new(onlyIntersection.GetPositionOnRoad(road), carMove.EndPosition,
                            carMove.CorrelatingIntersection)
                    };
                }

                var firstIntersection = viableIntersectionsOrderedByProximity[0];
                var traversedIntersectionPairs =
                    viableIntersectionsOrderedByProximity.Zip(viableIntersectionsOrderedByProximity.Skip(1));

                var pathsBetweenIntersectionPairs = traversedIntersectionPairs
                    .Select(intersections =>
                        new CarMovement(
                            intersections.First.GetPositionOnRoad(road),
                            intersections.Second.GetPositionOnRoad(road),
                            intersections.Second
                        )
                    ).ToList();

                var lastIntersection = pathsBetweenIntersectionPairs[^1].CorrelatingIntersection;

                return pathsBetweenIntersectionPairs
                    .Prepend(new CarMovement(carMove.StartPosition, firstIntersection.GetPositionOnRoad(road),
                        firstIntersection))
                    .Append(new CarMovement(lastIntersection.GetPositionOnRoad(road), carMove.EndPosition,
                        carMove.CorrelatingIntersection))
                    .ToList();
            }
        ).ToList();
    }

    public static List<CarMovement>? FindTheShortestPathTo(this Position startPosition, Position endPosition,
        double reserveRadius)
    {
        var path = FindShortestPath(startPosition, endPosition);
        if (path != null)
            path = SplitPathByIntersections(path, reserveRadius);
        return path;
    }

    private static List<CarMovement>? FindShortestPath(Position startPosition, Position endPosition,
        double currentPathLength = 0, Dictionary<Position, double>? shortestDistances = null)
    {
        if (startPosition.Road == endPosition.Road)
            return new List<CarMovement> {new(startPosition, endPosition, null)};

        shortestDistances ??= new Dictionary<Position, double>();

        if (shortestDistances.TryGetValue(startPosition, out var recordedPathLength))
        {
            if (recordedPathLength <= currentPathLength || recordedPathLength.AlmostEqualTo(currentPathLength))
                return null;

            shortestDistances[startPosition] = currentPathLength;
        }
        else
        {
            shortestDistances.Add(startPosition, currentPathLength);
        }

        var paths = startPosition.Road.IntersectionsWithOtherRoads
            .Select(intersection =>
            {
                var currentRoad = startPosition.Road;
                var nextRoad = intersection.GetRoadOppositeFrom(currentRoad);
                var traveledDistance = currentRoad
                    .GetShortestPath(startPosition.Offset, intersection.GetOffsetOfRoad(currentRoad)).Distance;

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

        return paths.MinByOrDefault(tuple => tuple.pathLength)?.path;
    }
}