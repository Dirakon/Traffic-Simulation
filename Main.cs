using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

public partial class Main : Node3D
{
    private static Main Instance;
    [Export] private Array<NodePath> inEditorRoads;
    private List<Road> roads;

    public static Position GetRandomPosition()
    {
        var randomRoad = Instance.roads.Random();
        var potentialPosition = new Position(
            new Random().NextDouble() * randomRoad.GetMaxOffset(),
            randomRoad
        );
        if (potentialPosition.Road.intersectionsWithOtherRoads.IsEmpty())
            return potentialPosition;
        var closestIntersectionDistance = potentialPosition.Road.intersectionsWithOtherRoads.Select(intersection =>
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
        Instance = this;
        roads = inEditorRoads.Select(GetNode<Road>).ToList();
        roads.ForEach(road =>
        {
            roads.ForEach(otherRoad =>
            {
                if (otherRoad == road)
                    return;
                var intersectionsAlreadyFound =
                    road.intersectionsWithOtherRoads
                        .Any(readyIntersection => readyIntersection.GetRoadOppositeFrom(road) == otherRoad);
                if (intersectionsAlreadyFound)
                    return;

                var intersections = road.GetIntersectionsWith(otherRoad);
                road.intersectionsWithOtherRoads.AddRange(intersections);
                otherRoad.intersectionsWithOtherRoads.AddRange(intersections);
            });
        });
        ValidateRoadNetwork();
    }

    private void ValidateRoadNetwork()
    {
        var intersectionDistances = roads
            .SelectMany(road =>
                road.intersectionsWithOtherRoads.SelectMany(firstIntersection =>
                    road.intersectionsWithOtherRoads
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