using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Car : PathFollow3D
{
    [Export] public InEditorPosition _inEditorEndPosition;
    private Position currentPosition;
    public Position? endPosition;
    private List<CarMovement> currentPath;
    
    [Export] private double speed;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        endPosition = _inEditorEndPosition.GetPosition(this);

        var road = GetParent() as Road;
        var offset = road.GetOffsetOfGivenGlobalPosition(GlobalPosition);
        currentPosition = new Position(offset,road);

        GD.Print(endPosition.Value.Road.Name);
    }

    // TODO: replace ticks with signals from Main signifying the start of simulation
    public int ticksLeft = 0;
    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (ticksLeft-- == 0)
        {
            GD.Print(FindShortestPath(currentPosition,endPosition.Value).Count);
            var path = FindShortestPath(currentPosition, endPosition.Value);
            path.ForEach(
                order =>
                {
                    GD.Print(order);
                });
            currentPath = path;
        }
        if (currentPath.Count == 0) return;
        // Move forward
        
        var currentGoal = currentPath[0].EndPosition; 
        var direction = Math.Sign(currentGoal.Offset - currentPosition.Offset);
        currentPosition.Offset += direction * delta * speed;

        var newDirection = Math.Sign(currentGoal.Offset - currentPosition.Offset);

        Progress = (float) currentPosition.Offset;
        
        if (newDirection == direction && direction != 0) return;
        // When movement finished, go to the next order
        
        currentPath.RemoveAt(0);

        if (currentPath.Count == 0) return;
        // When there is a next order, reparent to the new road and prepare for the movement.

        var newParent = currentPath[0].StartPosition.Road;
        var oldParent = currentPosition.Road;
            
        oldParent.RemoveChild(this);
        newParent.AddChild(this);

        currentPosition = currentPath[0].StartPosition;
        Progress = (float)currentPosition.Offset;
    }

    public static List<CarMovement>? FindShortestPath(Position startPosition, Position endPosition, double currentPathLength = 0, int iterations = 0,Dictionary<Position,double> shortestDistances = null)
    {
        if (startPosition.Road == endPosition.Road) 
            return new List<CarMovement> {new(startPosition, endPosition)};
        
        if (shortestDistances == null)
        {
            shortestDistances = new Dictionary<Position, double>();
        }

        if (shortestDistances.TryGetValue(startPosition,out var recordedPathLength))
        {
            const double epsilon = 0.0001;
            if (recordedPathLength <= currentPathLength + epsilon)
            {
                return null;
            }

            shortestDistances[startPosition]=currentPathLength;
        }
        else
        {
            shortestDistances.Add(startPosition,currentPathLength);
        }
        
        var paths = startPosition.Road.intersectionsWithOtherRoads
            .Select(intersection =>
            {
                var traveledDistance = Math.Abs(intersection.OurOffset - startPosition.Offset);
                var pathFromIntersection = FindShortestPath(intersection.AsOtherRoadPosition(), endPosition,
                    currentPathLength + traveledDistance, iterations+1,shortestDistances);
                (RoadIntersection intersection, List<CarMovement> pathFromIntersection, double pathLength)? tuple =
                    pathFromIntersection == null ? null : (intersection, pathFromIntersection, currentPathLength + traveledDistance);
                return tuple;
            })
            .NotNull()
            .Select(tuple =>
                (tuple.pathLength,path:tuple.pathFromIntersection
                    .Prepend(new CarMovement(startPosition, tuple.intersection.AsOurRoadPosition())).ToList())
            );

        var enumeratedPaths = paths.ToList();
        return enumeratedPaths.Count == 0 ? null : enumeratedPaths.MinBy(tuple => tuple.pathLength).path;
    }
}