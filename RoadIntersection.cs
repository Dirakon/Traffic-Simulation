using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class RoadIntersection
{
    private const double MoveFactor = 0.5f;
    private Road road1, road2;
    private double offset1, offset2;

    private List<RoadIntersectionExit> exits;

    public double GetOffsetOfRoad(Road road)
    {
        if (road == road1)
            return offset1;
        if (road == road2)
            return offset2;
        throw new ArgumentException("Road intersection tries to get the offset of an unknown road");
    }
    
    public RoadIntersection(Road road1, double offset1, Road road2, double offset2)
    {
        this.road1 = road1;
        this.road2 = road2;
        this.offset1 = offset1;
        this.offset2 = offset2;

        this.exits = new List<RoadIntersectionExit>();
        var unsuccessfullyProbedExits = new List<(Road, int)>();
        foreach (var (road, offset) in new List<(Road, double)> {(road1, offset1), (road2, offset2)})
        {
            var maxOffset = road.GetMaxOffset();
            var startingPoint = road.OffsetToPosition(offset);
            foreach (var probeDirection in new List<int>{-1, 1})
            {
                var movedOffset = offset + probeDirection*MoveFactor;
                if (road.IsEnclosed())
                    movedOffset = movedOffset % maxOffset;
                else if (movedOffset < 0 || movedOffset > maxOffset)
                {
                    unsuccessfullyProbedExits.Add((road,probeDirection));
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
    
    
    
    public Road GetRoadOppositeFrom(Road road)
    {
        if (road == road1)
            return road2;
        if (road == road2)
            return road1;
        throw new ArgumentException("Road intersection tries to get the opposite of an unknown road");
    }
}

record RoadIntersectionExit(Vector3 WorldDirection, Road Road, int RoadDirection)
{
}

public class UncontrolledIntersection : RoadIntersection{
    public UncontrolledIntersection(Road road1, double offset1, Road road2, double offset2) : base(road1, offset1, road2, offset2)
    {
    }
}

public class TrafficLightControlledIntersection : RoadIntersection{
    public TrafficLightControlledIntersection(Road road1, double offset1, Road road2, double offset2) : base(road1, offset1, road2, offset2)
    {
    }
}