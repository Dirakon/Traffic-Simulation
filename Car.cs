using System;
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

        var path = GetParent() as Road;
        var offset = path.GetOffsetOfGivenGlobalPosition(GlobalPosition);
        currentPosition = new Position(offset,path);

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
        
        var (_, currentGoal) = currentPath[0]; 
        var direction = Math.Sign(currentGoal.Offset - currentPosition.Offset);
        currentPosition.Offset += direction * delta * speed;

        var newDirection = Math.Sign(currentGoal.Offset - currentPosition.Offset);

        Progress = (float) currentPosition.Offset;
        
        if (newDirection == direction) return;
        // When movement finished, go to the next order
        
        currentPath.RemoveAt(0);

        if (currentPath.Count == 0) return;
        // When there is a next order, reparent to the new road and prepare for the movement.

        var newParent = currentPath[0].StartPosition.Road;
        var oldParent = currentPosition.Road;
            
        oldParent.RemoveChild(this);
        newParent.AddChild(this);

        currentPosition = currentPath[0].StartPosition;
    }

    // TODO: actually make path the shortest (for now, we just find an arbitrary path, with infinite cycle when path doesn't exist)
    public static List<CarMovement>? FindShortestPath(Position startPosition, Position endPosition, int iterations = 0)
    {
        if (iterations > 3)
            return null;
        if (startPosition.Road == endPosition.Road) 
            return new List<CarMovement> {new(startPosition, endPosition)};
        
        var paths = startPosition.Road.intersectionsWithOtherRoads
            .Select(intersection =>
            {
                var pathFromIntersection = FindShortestPath(intersection.AsOtherRoadPosition(), endPosition, iterations+1);
                (RoadIntersection intersection, List<CarMovement> pathFromIntersection)? tuple =
                    pathFromIntersection == null ? null : (intersection, pathFromIntersection);
                return tuple;
            })
            .NotNull()
            .Select(tuple =>
                tuple.pathFromIntersection
                    .Prepend(new CarMovement(startPosition, tuple.intersection.AsOurRoadPosition())).ToList()
            );

        return paths.FirstOrDefault();
    }
}