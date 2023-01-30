using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Car : PathFollow3D
{
    [Export] public PackedScene CarVisualsPrefab;
    private List<CarMovement>? currentPath;
    private Position currentPosition;

    [Export] public float PositiveDirectionHOffset,
        NegativeDirectionHOffset,
        PositiveParkedHOffset,
        NegativeParkedHOffset;


    private List<RoadIntersection> reservedIntersections = new();

    [Export] private double ReserveRadius;

    [Export] private double speed;

    private CarState state = new CarJustSpawned();

    public void CreateVisuals()
    {
        var visuals = CarVisualsPrefab.Instantiate() as CarVisuals;
        visuals.Init(this);
        GetTree().CurrentScene.AddChild(visuals);
        visuals.GlobalPosition = GlobalPosition;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        var road = GetParent() as Road ??
                   throw new InvalidOperationException($"{Name} does not have initial road as the parent");
        var offset = road.PositionToOffset(GlobalPosition);
        currentPosition = new Position(offset, road);

        CallDeferred(MethodName.CreateVisuals);
    }

    private void GetOnTheRoad(Position position, int direction)
    {
        var newParent = position.Road;
        var oldParent = GetParent() as Road ??
                        throw new InvalidOperationException(
                            $"{Name} is in invalid state: it does not belong to any road");

        if (oldParent != newParent)
        {
            oldParent.RemoveChild(this);
            newParent.AddChild(this);
        }

        Progress = (float) position.Offset;
        HOffset = direction == -1 ? NegativeDirectionHOffset : PositiveDirectionHOffset;
        //GD.Print($"{Name} on the road! THe direction is {direction}");
    }

    private void Park()
    {
        const double epsilon = 0.0001;
        if (Math.Abs(HOffset - PositiveDirectionHOffset) < epsilon) HOffset = PositiveParkedHOffset;
        if (Math.Abs(HOffset - NegativeDirectionHOffset) < epsilon) HOffset = NegativeParkedHOffset;
    }

    private void UnPark()
    {
        const double epsilon = 0.0001;
        if (Math.Abs(HOffset - PositiveParkedHOffset) < epsilon) HOffset = PositiveDirectionHOffset;
        if (Math.Abs(HOffset - NegativeParkedHOffset) < epsilon) HOffset = NegativeDirectionHOffset;
    }

    private void TakeAppropriateRoadSide(int direction)
    {
        HOffset = direction == -1 ? NegativeDirectionHOffset : PositiveDirectionHOffset;
    }

    private bool CanClaimSpot(ReservedCarSpot potentialSpot)
    {
        return potentialSpot.RoadToReserve.ReservedCarSpots.All(spot =>
            spot.Direction != potentialSpot.Direction ||
            spot.ReservingCar == this ||
            spot.GetStartingOffset() > potentialSpot.GetEndingOffset() ||
            spot.GetEndingOffset() < potentialSpot.GetStartingOffset()
        );
    }

    // private void ClaimSpot(ReservedCarSpot spot)
    // {
    //     var spotClaimedOnTheSameRoad =
    //         ReservedCarSpots.Find(claimedSpot => spot.RoadToReserve == claimedSpot.RoadToReserve);
    //     if (spotClaimedOnTheSameRoad != null)
    //     {
    //         ReservedCarSpots.Remove(spotClaimedOnTheSameRoad);
    //         spot.RoadToReserve.ReservedCarSpots.Remove(spotClaimedOnTheSameRoad);
    //     }
    //     spot.RoadToReserve.ReservedCarSpots.Add(spot);
    //     ReservedCarSpots.Add(spot);
    // }
    //
    // private void UnClaimSpot(ReservedCarSpot spot)
    // {
    //     ReservedCarSpots.Remove(spot);
    //     spot.RoadToReserve.ReservedCarSpots.Remove(spot);
    // }

    // private void AttemptToClaimTheInitialRoad(CarMovement initialMoveOrder)
    // {
    //     var spotToClaim = new ReservedCarSpot(this, currentPosition.Offset, ReserveRadius, currentPosition.Road, initialMoveOrder.GetDirection());
    //     if (CanClaimSpot(spotToClaim))
    //     {
    //         ClaimSpot(spotToClaim);
    //         GetOnTheRoad(currentPosition,initialMoveOrder.GetDirection());
    //     }
    // }

    private bool TryFindRandomPath()
    {
        currentPath = FindShortestPath(currentPosition, Main.GetRandomPosition());
        if (currentPath is not {Count: > 0}) return false;

        currentPath.ForEach(el => GD.Print(el.ToString()));
        GD.Print("after");
        currentPath = SplitPathByIntersections(currentPath);
        currentPath.ForEach(el => GD.Print(el.ToString()));
        return true;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        switch (state)
        {
            case CarJustSpawned:
                if (TryFindRandomPath())
                {
                    var firstOrder = currentPath[0];
                    state = new CarParkedToClaimLane(new ReservedCarSpot(this, firstOrder.StartPosition.Offset,
                        ReserveRadius, firstOrder.GetRoad(), firstOrder.GetDirection()));
                }

                break;
            case CarMoving(ReservedCarSpot reservedSpot):
                var currentOrder = currentPath[0];
                var distanceToGoal = Math.Abs(reservedSpot.Offset - currentOrder.EndPosition.Offset);
                var newOffset = currentPosition.Offset +
                                currentOrder.GetDirection() * Math.Min(delta * speed, distanceToGoal);
                var movedSpot = new ReservedCarSpot(this, newOffset, ReserveRadius, currentOrder.GetRoad(),
                    currentOrder.GetDirection());

                break;
            case CarParkedToClaimLane(ReservedCarSpot spotToClaim):
                if (CanClaimSpot(spotToClaim))
                {
                    // ClaimSpot(spotToClaim);
                    GetOnTheRoad(spotToClaim.GetPosition(), spotToClaim.Direction);
                    state = new CarMoving(spotToClaim);
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state));
        }
        //
        // if (currentPath == null || currentPath.IsEmpty())
        // {
        //     currentPath = FindShortestPath(currentPosition, Main.GetRandomPosition());
        //     if (currentPath == null || currentPath.IsEmpty() || currentRoadReservedSpot == null)
        //         return;
        //     
        //     // Check if the first order is to change the lane on the current road
        //     if (currentRoadReservedSpot.Direction != currentPath[0].GetDirection())
        //     {
        //         UnClaimSpot(currentRoadReservedSpot);
        //         Park();
        //     }
        //     
        //     return;
        // }
        //
        // if (currentRoadReservedSpot == null)
        // {
        //     // No spots are reserved at all, meaning the car is not yet on the road.
        //     AttemptToClaimTheInitialRoad(currentPath[0]);
        //     return;
        // }
        //
        // var currentGoal = currentPath[0].EndPosition; 
        // var direction = Math.Sign(currentGoal.Offset - currentPosition.Offset);
        // var newOffset = currentPosition.Offset + direction * delta * speed;
        // var newDirection = Math.Sign(currentGoal.Offset - newOffset);
        //
        // var newSpot = new ReservedCarSpot(this, newOffset, ReserveRadius, currentGoal.Road, currentPath[0].GetDirection());
        //
        //
        // if (!CanClaimSpot(newSpot))
        //     return;
        //
        // if (ShouldClaimFutureSpot(currentPath))
        // {
        //     var futureSpot = GetFutureSpot(currentPath);
        //     if (!CanClaimSpot(futureSpot))
        //         return;
        //     ClaimSpot(futureSpot);
        // }
        //
        // ClaimSpot(newSpot);
        // UnPark();
        // currentPosition.Offset = newOffset;
        // Progress = (float) currentPosition.Offset;
        //
        // RemoveInvalidReservedSpots(currentPath);
        //
        // var isCurrentGoalReached = newDirection != direction || direction == 0;
        // if (isCurrentGoalReached)
        // {
        //     currentPath.RemoveAt(0);
        //
        //     if (!currentPath.IsEmpty())
        //     {
        //         // When there is a next order, reparent to the new road and prepare for the movement.
        //         GetOnTheRoad(currentPath[0].StartPosition, currentPath[0].GetDirection());
        //
        //         currentPosition = currentPath[0].StartPosition;
        //         Progress = (float)currentPosition.Offset;
        //     }
        //     
        // }
        // else
        // {
        //     TakeAppropriateRoadSide(newDirection);
        // }
    }

    // private void RemoveInvalidReservedSpots(List<CarMovement> carMovementOrders)
    // {
    //     var currentRoadSpot = ReservedCarSpots
    //         .SingleOrDefault(spot=>spot.RoadToReserve==carMovementOrders[0].GetRoad());
    //     var futureRoadSpot = carMovementOrders.Count <= 1 ?null:
    //         ReservedCarSpots
    //             .SingleOrDefault(spot=>spot.RoadToReserve==carMovementOrders[1].GetRoad());;
    //     var previousRoadSpot = ReservedCarSpots.
    //         SingleOrDefault(spot=> spot != currentRoadSpot && spot != futureRoadSpot);Car
    //     if (futureRoadSpot != null)
    //     {
    //         var distanceFromFutureSpot =
    //             Math.Abs(carMovementOrders[0].EndPosition.Offset - currentPosition.Offset);
    //         if (distanceFromFutureSpot > ReserveRadius)
    //         {
    //             UnClaimSpot(futureRoadSpot);
    //         }
    //     }
    //
    //     if (previousRoadSpot != null)
    //     {
    //         // TODO: fix a potential bug where if a road changes every (0; ReserveRadius), this previousRoadSpot won't be removed
    //         var distanceFromPreviousSpot =
    //             Math.Abs(carMovementOrders[0].StartPosition.Offset - currentPosition.Offset);
    //         if (distanceFromPreviousSpot > ReserveRadius)
    //         {
    //             UnClaimSpot(previousRoadSpot);
    //         }
    //     }
    // }

    private bool ShouldClaimFutureSpot(List<CarMovement> carMovementOrders)
    {
        if (carMovementOrders[0].EndPosition.Road != currentPosition.Road)
            GD.PushError("Unexpected behaviour! Car order list contains non-current road as the first order");
        if (carMovementOrders.Count <= 1)
            return false;
        var distanceFromFutureSpot =
            Math.Abs(carMovementOrders[0].EndPosition.Offset - currentPosition.Offset);
        return distanceFromFutureSpot <= ReserveRadius;
    }

    private ReservedCarSpot GetFutureSpot(List<CarMovement> carMovementOrders)
    {
        var futureRoad = carMovementOrders[1].GetRoad();
        var spotToClaim = new ReservedCarSpot(this, carMovementOrders[1].StartPosition.Offset, ReserveRadius,
            futureRoad, carMovementOrders[1].GetDirection());
        return spotToClaim;
    }


    public static List<CarMovement> SplitPathByIntersections(List<CarMovement> originalPath)
    {
        return originalPath.SelectMany(carMove =>
            {
                if (carMove.GetDirection() == 0)
                    return new List<CarMovement> {};
                var startingOffset = carMove.StartPosition.Offset;
                var direction = carMove.GetDirection();
                var road = carMove.GetRoad();
                var epsilon = 0.001;
                // TODO: instead of MinBy, sort them, and take until they're over the offset.
                var viableIntersectionsOrderedByProximity = new List<RoadIntersection>();
                if (carMove.GetRoad().IsEnclosed())
                    viableIntersectionsOrderedByProximity = road.intersectionsWithOtherRoads
                        .Where(intersection => intersection != carMove.IntersectionAtTheEnd)
                        .Where(intersection => Math.Abs(intersection.GetOffsetOfRoad(road) - startingOffset) > epsilon)
                        .Select(intersection =>
                        {
                            var loopNeeded = Math.Sign(
                                                 intersection.GetOffsetOfRoad(road) -
                                                 startingOffset) !=
                                             direction;
                            return loopNeeded ? // TODO: turn into ABS if working
                                (distance: road.GetMaxOffset() - Math.Abs(intersection.GetOffsetOfRoad(road) - startingOffset), intersection) 
                                : (distance: Math.Abs(intersection.GetOffsetOfRoad(road) - startingOffset), intersection);
                        }).Where(tuple => tuple.distance < carMove.GetDistance())
                        .OrderBy(tuple => tuple.distance)
                        .Select(tuple => tuple.intersection)
                        .ToList();
                else
                    viableIntersectionsOrderedByProximity = road.intersectionsWithOtherRoads
                        .Where(intersection => intersection != carMove.IntersectionAtTheEnd)
                        .Where(intersection => Math.Abs(intersection.GetOffsetOfRoad(road) - startingOffset) > epsilon)
                        .Where(intersection => Math.Sign(
                                                   intersection.GetOffsetOfRoad(road) - startingOffset) ==
                                               direction
                        )
                        .Select(intersection =>
                            (distance: Math.Abs(intersection.GetOffsetOfRoad(road) - startingOffset), intersection)
                        )
                        .Where(tuple => tuple.distance < carMove.GetDistance())
                        .OrderBy(tuple => tuple.distance)
                        .Select(tuple => tuple.intersection)
                        .ToList();

                if (viableIntersectionsOrderedByProximity.IsEmpty()) return new List<CarMovement> {carMove};
                if (viableIntersectionsOrderedByProximity.Count == 1)
                {
                    var onlyIntersection = viableIntersectionsOrderedByProximity[0];

                    return new List<CarMovement>
                    {
                        new(carMove.StartPosition, onlyIntersection.GetPositionOfRoad(road), onlyIntersection),
                        new(onlyIntersection.GetPositionOfRoad(road), carMove.EndPosition, carMove.IntersectionAtTheEnd)
                    };
                }

                var firstIntersection = viableIntersectionsOrderedByProximity[0];
                var traversedIntersectionPairs =
                    viableIntersectionsOrderedByProximity.Zip(viableIntersectionsOrderedByProximity.Skip(1));

                var pathsBetweenIntersectionPairs = traversedIntersectionPairs
                    .Select(intersections =>
                        new CarMovement(
                            intersections.First.GetPositionOfRoad(road),
                            intersections.Second.GetPositionOfRoad(road),
                            intersections.Second
                        )
                    ).ToList();

                var lastIntersection = pathsBetweenIntersectionPairs[^1].IntersectionAtTheEnd;
                // Else prepend and postpend
                return pathsBetweenIntersectionPairs
                    .Prepend(new CarMovement(carMove.StartPosition, firstIntersection.GetPositionOfRoad(road),
                        firstIntersection))
                    .Append(new CarMovement(lastIntersection.GetPositionOfRoad(road), carMove.EndPosition,
                        carMove.IntersectionAtTheEnd))
                    .ToList();
            }
        ).ToList();
    }

    public static List<CarMovement>? FindShortestPath(Position startPosition, Position endPosition,
        double currentPathLength = 0, Dictionary<Position, double>? shortestDistances = null)
    {
        const double epsilon = 0.0001;
        if (startPosition.Road == endPosition.Road)
            return new List<CarMovement> {new(startPosition, endPosition, null)};

        shortestDistances ??= new Dictionary<Position, double>();

        if (shortestDistances.TryGetValue(startPosition, out var recordedPathLength))
        {
            if (recordedPathLength <= currentPathLength + epsilon) return null;

            shortestDistances[startPosition] = currentPathLength;
        }
        else
        {
            shortestDistances.Add(startPosition, currentPathLength);
        }

        var paths = startPosition.Road.intersectionsWithOtherRoads
            .Select(intersection =>
            {
                var currentRoad = startPosition.Road;
                var nextRoad = intersection.GetRoadOppositeFrom(currentRoad);
                var traveledDistance = Math.Abs(intersection.GetOffsetOfRoad(currentRoad) - startPosition.Offset);

                var currentEnd = new Position(intersection.GetOffsetOfRoad(currentRoad), currentRoad);
                var nextStart = new Position(intersection.GetOffsetOfRoad(nextRoad), nextRoad);
                var pathFromIntersection = FindShortestPath(nextStart, endPosition,
                    currentPathLength + traveledDistance, shortestDistances);
                (RoadIntersection intersection, Position currentEnd, List<CarMovement> pathFromIntersection, double
                    pathLength)? tuple =
                        pathFromIntersection == null
                            ? null
                            : (intersection, currentEnd, pathFromIntersection, currentPathLength + traveledDistance);
                return tuple;
            })
            .NotNull()
            .Select(tuple =>
                (tuple.pathLength, path: tuple.pathFromIntersection
                    .Prepend(new CarMovement(startPosition, tuple.currentEnd, tuple.intersection)).ToList())
            );

        var enumeratedPaths = paths.ToList();
        return enumeratedPaths.IsEmpty() ? null : enumeratedPaths.MinBy(tuple => tuple.pathLength).path;
    }
}

internal abstract record CarState;

internal record CarJustSpawned : CarState;

internal record CarParkedToClaimLane(ReservedCarSpot SpotToClaim) : CarState;

internal record CarMoving(ReservedCarSpot CurrentRoadReservedSpot) : CarState;