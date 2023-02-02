using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public class RoadIntersection
{
    private const double EntranceTraversalFactor = 0.75f;
    public const double ProbeDistance = 0.5f;
    public const double IntersectionInteractionDistance = 2f;

    private readonly List<CarOnIntersectionEntry> CarEntries = new();
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
        foreach (var (road, startingOffset) in new List<(Road, double)> {(road1, offset1), (road2, offset2)})
        {
            var maxOffset = road.GetMaxOffset();
            var startingPosition = new Position(startingOffset, road);
            var startingPoint = road.OffsetToPosition(startingOffset);
            foreach (var probeDirection in new List<int> {-1, 1})
            {
                var movedPosition = startingPosition.MovedBy(ProbeDistance, probeDirection);
                if (movedPosition == null)
                {
                    unsuccessfullyProbedExits.Add((road, probeDirection));
                    continue;
                }

                var exitWorldDirection = startingPoint.DirectionTo(road.OffsetToPosition(movedPosition.Value.Offset));
                exits.Add(new RoadIntersectionExit(exitWorldDirection, road, probeDirection));
            }
        }

        // All unsuccessfully probed exits are assumed to have direction opposite to the entrance 
        foreach (var (road, probeDirection) in unsuccessfullyProbedExits)
        {
            GD.Print("An exit couldn't be probed, instead assuming the opposite of an entrance.");
            var oppositeExit = exits.Single(exit => road == exit.Road);
            exits.Add(new RoadIntersectionExit(-oppositeExit.WorldDirection, road, probeDirection));
        }
    }

    public Vector3 GlobalPosition { get; }

    public override string ToString()
    {
        return $"[from {road1.Name}({GetOffsetOfRoad(road1)}) to {road2.Name}({GetOffsetOfRoad(road2)}) ]";
    }

    public double GetOffsetOfRoad(Road road)
    {
        if (road == road1)
            return offset1;
        if (road == road2)
            return offset2;
        throw new ArgumentException("Road intersection tries to get the offset of an unknown road");
    }

    public bool ContainsAnExitToRoad(Road road)
    {
        return road == road1 || road == road2;
    }

    public Position GetPositionOnRoad(Road road)
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

    public bool TryReservePath(Car car, Road currentRoad, int currentDirection, Road roadAfterThis,
        int directionAfterThis)
    {
        if (CarEntries.Any(entry => entry.Car == car))
            GD.PushError($"Car {car.Name} tries to reserve an intersection path, already being there!");

        var exit = exits.Single(exit => exit.Road == roadAfterThis && exit.RoadDirection == directionAfterThis);
        var entrance = exits.Single(exit => exit.Road == currentRoad && exit.RoadDirection == -currentDirection);
        var spotsToClaim = new List<ReservedCarSpot>
        {
            new(
                car,
                Offset: GetPositionOnRoad(currentRoad).MovedByCapped(IntersectionInteractionDistance, entrance.RoadDirection).Offset,
                car.ReserveRadius,
                currentRoad,
                currentDirection
            ),
            new(
                car,
                Offset: GetPositionOnRoad(roadAfterThis).MovedByCapped(IntersectionInteractionDistance, exit.RoadDirection).Offset,
                car.ReserveRadius,
                roadAfterThis,
                directionAfterThis
            )
        };
        var canReserve = spotsToClaim.All(spot => spot.CanBeClaimed());
        if (!canReserve)
            return false;

        var potentialEntry =
            new CarOnIntersectionEntry(
                car,
                spotsToClaim[0],
                spotsToClaim[1],
                entrance,
                exit,
                spotsToClaim[0].GetPosition(),
                GetPositionOnRoad(currentRoad)
                    with
                    {
                        Offset = GetPositionOnRoad(currentRoad).MovedByCapped(IntersectionInteractionDistance * (1 - EntranceTraversalFactor),entrance.RoadDirection).Offset
                    }
            );

        var sortedExits = exits
            .Where(exit => exit != entrance)
            .Select(exit =>
            {
                double signedAngle = entrance.WorldDirection.SignedAngleTo(exit.WorldDirection, Vector3.Up);
                if (signedAngle < 0)
                    signedAngle = 2 * Math.PI - signedAngle;
                return (exit, angularDistance: signedAngle);
            })
            .OrderBy(tuple => tuple.angularDistance)
            .Select(tuple => tuple.exit)
            .ToList();
        var rightExit = sortedExits[0];
        var straightExit = sortedExits[1];
        var leftExit = sortedExits[2];

        if (exit == straightExit)
        {
            // Going straight is blocked by 
            //  1. Any movement from right
            if (CarEntries.Any(entry => entry.Entrance == rightExit))
                return false;
            //  2. Movement from straight to right (i.e. straight's immediate left)
            if (CarEntries.Any(entry => entry.Entrance == straightExit && entry.PlannedExit == rightExit))
                return false;
            // 3. Movement from left to right (i.e. left's straight)
            if (CarEntries.Any(entry => entry.Entrance == leftExit && entry.PlannedExit == rightExit))
                return false;
        }
        else if (exit == rightExit)
        {
            // Going to immediate right is unblockable
        }
        else if (exit == leftExit)
        {
            // Going to immediate left is blocked by all movement except from left to entrance (i.e. left's immediate right)
            if (!CarEntries.All(entry => entry.Entrance == leftExit && entry.PlannedExit == entrance))
                return false;
        }
        else
        {
            // TODO: Investigate why cars want to turn around so often
            // GD.Print($"Was trynna go from {entrance} to {exit}. The intersection is {this}");
            // GD.Print($"{straightExit}, {rightExit}, {entrance}, {leftExit}");
            // return true;
            // throw new ArgumentOutOfRangeException();
        }


        spotsToClaim.ForEach(spot => spot.RegisterClaim(false));
        CarEntries.Add(
            potentialEntry
        );

        return true;
    }


    public Position GetCurrentCarPosition(Car car)
    {
        var entry = CarEntries.Single(entry => entry.Car == car);
        return entry.CurrentPosition;
    }

    public int GetCurrentCarDirection(Car car)
    {
        var entry = CarEntries.Single(entry => entry.Car == car);
        return (entry.Entering ? entry.EntranceSpot : entry.ExitSpot).Direction;
    }

    public void AppropriatelyMoveCar(Car car, double maxCarMovement)
    {
        var epsilon = 0.001;
        
        var entry = CarEntries.Single(entry => entry.Car == car);
        var spot = entry.Entering ? entry.EntranceSpot : entry.ExitSpot;
        
        SingleRoadPath currentSingleRoadPath = entry.CurrentPosition.GetSingleRoadPathWithSetDirection(entry.CurrentDestination,spot.Direction)!;
        
        var distanceToGoal = currentSingleRoadPath.Distance;
        Position movedPosition = entry.CurrentPosition.MovedBy(Math.Min(maxCarMovement, distanceToGoal),
            spot.Direction)!.Value;
        var newDistanceToGoal = movedPosition.GetSingleRoadPathWithSetDirection(entry.CurrentDestination,spot.Direction)!
            .Distance;

        if (entry.Entering && newDistanceToGoal < epsilon)
        {
            entry.Entering = false;
            entry.CurrentDestination = entry.ExitSpot.GetPosition();
            entry.CurrentPosition = GetPositionOnRoad(entry.CurrentDestination.Road);
        }
        else
        {
            entry.CurrentPosition = movedPosition;
        }
    }

    public ReservedCarSpot? IntersectionPassed(Car car)
    {
        var epsilon = 0.001;
        var entry = CarEntries.Single(entry => entry.Car == car);
        var distanceToGoal = Math.Abs(entry.CurrentDestination.Offset - entry.CurrentPosition.Offset);
        if (!entry.Entering && distanceToGoal < epsilon)
        {
            entry.EntranceSpot.UnregisterClaim();
            CarEntries.Remove(entry);
            return entry.ExitSpot;
        }

        return null;
    }
}

internal class CarOnIntersectionEntry
{
    public Car Car;
    public Position CurrentPosition, CurrentDestination;
    public bool Entering = true;
    public ReservedCarSpot EntranceSpot, ExitSpot;
    public RoadIntersectionExit PlannedExit, Entrance;

    public CarOnIntersectionEntry(Car car, ReservedCarSpot entranceSpot, ReservedCarSpot exitSpot,
        RoadIntersectionExit entrance, RoadIntersectionExit plannedExit, Position currentPosition,
        Position currentDestination)
    {
        EntranceSpot = entranceSpot;
        ExitSpot = exitSpot;
        Car = car;
        PlannedExit = plannedExit;
        CurrentPosition = currentPosition;
        CurrentDestination = currentDestination;
        Entrance = entrance;
    }
}

internal record RoadIntersectionExit(Vector3 WorldDirection, Road Road, int RoadDirection)
{
    public override string ToString()
    {
        return $"{Road.Name} ({RoadDirection})";
    }
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