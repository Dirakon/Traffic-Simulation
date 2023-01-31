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
        var potentialPosition =  new Position(
            new Random().NextDouble() * randomRoad.GetMaxOffset(),
            randomRoad
        );
        if (potentialPosition.Road.intersectionsWithOtherRoads.IsEmpty())
            return potentialPosition;
        var closestIntersectionDistance = potentialPosition.Road.intersectionsWithOtherRoads.Select(intersection =>
            potentialPosition.Road.IsEnclosed()
                ? Math.Min(
                    potentialPosition.Road.GetMaxOffset() - Math.Abs(potentialPosition.Offset -
                                                                     intersection.GetOffsetOfRoad(
                                                                         potentialPosition.Road)),
                    Math.Abs(potentialPosition.Offset - intersection.GetOffsetOfRoad(potentialPosition.Road))
                )
                : Math.Abs(potentialPosition.Offset - intersection.GetOffsetOfRoad(potentialPosition.Road))
        ).Min();
        if (RoadIntersection.IntersectionInteractionDistance >= closestIntersectionDistance)
        {
            GD.Print("Tried to generate a position, but it is too close to an intersection!");
            return GetRandomPosition();
        }
        else
        {
            return potentialPosition;
        }
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
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}