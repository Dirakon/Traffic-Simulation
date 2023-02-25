using Godot;

namespace TrafficSimulation.scripts;

public record struct CarMovement
{
    public CarMovement(Position startPosition, Position endPosition, RoadIntersection? correlatingIntersection)
    {
        StartPosition = startPosition;
        EndPosition = endPosition;
        CorrelatingIntersection = correlatingIntersection;

        var path = startPosition.Road.GetShortestPath(StartPosition.Offset, EndPosition.Offset);

        Direction = path.Direction;
        Distance = path.Distance;

        if (startPosition.Road != endPosition.Road)
            GD.PushError($"Internal assumption that CarMovement is a path inside a single road is ruined by {this}");
    }

    public Position StartPosition { get; }
    public Position EndPosition { get; }
    public int Direction { get; }
    public double Distance { get; }
    public RoadIntersection? CorrelatingIntersection { get; }

    public override string ToString()
    {
        return $"From {{{StartPosition}}} to {{{EndPosition}}}. The direction is {Direction}" +
               $". The intersection is {{{CorrelatingIntersection}}}";
    }


    public Road GetRoad()
    {
        return StartPosition.Road;
    }
}