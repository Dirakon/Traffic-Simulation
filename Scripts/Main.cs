using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using TrafficSimulation.scripts.extensions;

namespace TrafficSimulation.scripts;

public partial class Main : Node3D
{
    private static Main _instance;
    [Export] private Array<NodePath> _inEditorRoads;
    private List<Road> _roads;

    public static Position GetRandomPosition()
    {
        var randomRoad = _instance._roads.Random();
        var potentialPosition = new Position(
            new Random().NextDouble() * randomRoad.GetMaxOffset(),
            randomRoad
        );
        if (potentialPosition.Road.IntersectionsWithOtherRoads.IsEmpty())
            return potentialPosition;
        var closestIntersectionDistance = potentialPosition.Road.IntersectionsWithOtherRoads.Select(intersection =>
            randomRoad.GetShortestPath(intersection.GetOffsetOfRoad(randomRoad), potentialPosition.Offset).Distance
        ).Min();
        if (RoadIntersection.IntersectionInteractionDistance >= closestIntersectionDistance)
        {
            GD.Print("Tried to generate a position, but it is too close to an intersection!");
            return GetRandomPosition();
        }

        return potentialPosition;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _instance = this;
        _roads = _inEditorRoads.Select(GetNode<Road>).ToList();
        _roads.ForEach(road =>
        {
            _roads.ForEach(otherRoad =>
            {
                if (otherRoad == road)
                    return;
                var intersectionsAlreadyFound =
                    road.IntersectionsWithOtherRoads
                        .Any(readyIntersection => readyIntersection.GetRoadOppositeFrom(road) == otherRoad);
                if (intersectionsAlreadyFound)
                    return;

                var intersections = road.GetIntersectionsWith(otherRoad);
                road.IntersectionsWithOtherRoads.AddRange(intersections);
                otherRoad.IntersectionsWithOtherRoads.AddRange(intersections);
            });
        });
        ValidateRoadNetwork();
    }

    private void ValidateRoadNetwork()
    {
        var intersectionDistances = _roads
            .SelectMany(road =>
                road.IntersectionsWithOtherRoads.SelectMany(firstIntersection =>
                    road.IntersectionsWithOtherRoads
                        .Where(secondIntersection => secondIntersection != firstIntersection)
                        .Select(secondIntersection => (
                                distance: road.GetShortestPath(firstIntersection.GetOffsetOfRoad(road),
                                    secondIntersection.GetOffsetOfRoad(road)).Distance,
                                intersectionDescription: $"{firstIntersection}-{secondIntersection}"
                            )
                        )
                )
            ).ToList();
        if (intersectionDistances.IsEmpty())
            return;
        var (distance, intersectionDescription) = intersectionDistances.MinBy(tuple => tuple.distance);
        if (distance <= RoadIntersection.IntersectionInteractionDistance)
            GD.PushError(
                $"Intersections are too close for stable work! Min distance is {distance} with {intersectionDescription}");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}