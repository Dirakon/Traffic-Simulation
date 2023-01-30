using System;
using Godot;

public record struct CarMovement
{
    public CarMovement(Position startPosition, Position endPosition, RoadIntersection? intersectionAtTheEnd)
    {
        StartPosition = startPosition;
        EndPosition = endPosition;
        IntersectionAtTheEnd = intersectionAtTheEnd;

        if (startPosition.Road != endPosition.Road)
            GD.PushError($"Internal assumption that CarMovement is a path inside a single road is ruined by {this}");
    }

    public Position StartPosition { get; }
    public Position EndPosition { get; }
    public RoadIntersection? IntersectionAtTheEnd { get; }

    public override string ToString()
    {
        return $"From {{{StartPosition}}} to {{{EndPosition}}}. The direction is {GetDirection()}" +
               $". The intersection is {{{IntersectionAtTheEnd}}}";
    }

    public double GetDistance()
    {
        return Math.Abs(EndPosition.Offset - StartPosition.Offset);
    }

    public void Deconstruct(out Position StartPosition, out Position EndPosition)
    {
        StartPosition = this.StartPosition;
        EndPosition = this.EndPosition;
    }

    public int GetDirection()
    {
        return Math.Sign(EndPosition.Offset - StartPosition.Offset);
    }

    public Road GetRoad()
    {
        return StartPosition.Road;
    }
}