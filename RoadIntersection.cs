using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class RoadIntersection
{
    private const double MoveFactor = 0.5f;

    private readonly List<RoadIntersectionExit> exits;
    private readonly double offset1;
    private readonly double offset2;
    private readonly Road road1;
    private readonly Road road2;


    public RoadIntersection(Road road1, double offset1, Road road2, double offset2)
    {
        this.road1 = road1;
        this.road2 = road2;
        this.offset1 = offset1;
        this.offset2 = offset2;

        GlobalPosition = road1.OffsetToPosition(offset1);

        exits = new List<RoadIntersectionExit>();
        var unsuccessfullyProbedExits = new List<(Road, int)>();
        foreach (var (road, offset) in new List<(Road, double)> {(road1, offset1), (road2, offset2)})
        {
            var maxOffset = road.GetMaxOffset();
            var startingPoint = road.OffsetToPosition(offset);
            foreach (var probeDirection in new List<int> {-1, 1})
            {
                var movedOffset = offset + probeDirection * MoveFactor;
                if (road.IsEnclosed())
                {
                    movedOffset = movedOffset % maxOffset;
                }
                else if (movedOffset < 0 || movedOffset > maxOffset)
                {
                    unsuccessfullyProbedExits.Add((road, probeDirection));
                    continue;
                }

                var exitWorldDirection = startingPoint.DirectionTo(road.OffsetToPosition(movedOffset));
                exits.Add(new RoadIntersectionExit(exitWorldDirection, road, probeDirection));
            }
        }

        // All unsuccessfully probed exits are assumed to have direction opposite to the entrance 
        foreach (var (road, probeDirection) in unsuccessfullyProbedExits)
        {
            var oppositeExit = exits.Single(exit => road == exit.Road);
            exits.Add(new RoadIntersectionExit(-oppositeExit.WorldDirection, road, probeDirection));
        }
    }

    public Vector3 GlobalPosition { get; }

    public override string ToString()
    {
        return $"[from {road1.Name} to {road2.Name}]";
    }

    public double GetOffsetOfRoad(Road road)
    {
        if (road == road1)
            return offset1;
        if (road == road2)
            return offset2;
        throw new ArgumentException("Road intersection tries to get the offset of an unknown road");
    }

    public Position GetPositionOfRoad(Road road)
    {
        return new Position(GetOffsetOfRoad(road), road);
    }


    public Road GetRoadOppositeFrom(Road road)
    {
        if (road == road1)
            return road2;
        if (road == road2)
            return road1;
        throw new ArgumentException("Road intersection tries to get the opposite of an unknown road");
    }
}

internal record RoadIntersectionExit(Vector3 WorldDirection, Road Road, int RoadDirection)
{
}

public class UncontrolledIntersection : RoadIntersection
{
    public UncontrolledIntersection(Road road1, double offset1, Road road2, double offset2) : base(road1, offset1,
        road2, offset2)
    {
    }
}

public class TrafficLightControlledIntersection : RoadIntersection
{
    public TrafficLightControlledIntersection(Road road1, double offset1, Road road2, double offset2) : base(road1,
        offset1, road2, offset2)
    {
    }
}