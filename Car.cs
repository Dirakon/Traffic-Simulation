using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Car : PathFollow3D
{
    [Export] public InEditorPosition _inEditorEndPosition;
    [Export] public float PositiveDirectionHOffset, NegativeDirectionHOffset;
    private Position currentPosition;
    public Position? endPosition;
    private List<CarMovement>? currentPath;
    
    [Export] private double speed;
    private List<ReservedCarSpot> ReservedCarSpots = new List<ReservedCarSpot>();

    [Export] private double ReserveRadius;
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        endPosition = _inEditorEndPosition.GetPosition(this);

        var road = GetParent() as Road ?? throw new InvalidOperationException($"{Name} does not have initial road as the parent");
        var offset = road.GetOffsetOfGivenGlobalPosition(GlobalPosition);
        currentPosition = new Position(offset,road);
    }

    private void GetOnTheRoad(Position position, int direction)
    {
        var newParent = position.Road;
        var oldParent = GetParent() as Road ?? throw new InvalidOperationException($"{Name} is in invalid state: it does not belong to any road");

        if (oldParent != newParent)
        {
            oldParent.RemoveChild(this);
            newParent.AddChild(this);
        }
        Progress = (float)position.Offset;
        HOffset = direction == -1 ? NegativeDirectionHOffset : PositiveDirectionHOffset;
        //GD.Print($"{Name} on the road! THe direction is {direction}");
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

    private void ClaimSpot(ReservedCarSpot spot)
    {
        var spotClaimedOnTheSameRoad =
            ReservedCarSpots.Find(claimedSpot => spot.RoadToReserve == claimedSpot.RoadToReserve);
        if (spotClaimedOnTheSameRoad != null)
        {
            ReservedCarSpots.Remove(spotClaimedOnTheSameRoad);
            spot.RoadToReserve.ReservedCarSpots.Remove(spotClaimedOnTheSameRoad);
        }
        spot.RoadToReserve.ReservedCarSpots.Add(spot);
        ReservedCarSpots.Add(spot);
    }

    private void UnclaimSpot(ReservedCarSpot spot)
    {
        ReservedCarSpots.Remove(spot);
        spot.RoadToReserve.ReservedCarSpots.Remove(spot);
    }

    private void AttemptToClaimTheInitialRoad(CarMovement initialMoveOrder)
    {
        var spotToClaim = new ReservedCarSpot(this, currentPosition.Offset, ReserveRadius, currentPosition.Road, initialMoveOrder.GetDirection());
        if (CanClaimSpot(spotToClaim))
        {
            ClaimSpot(spotToClaim);
            GetOnTheRoad(currentPosition,initialMoveOrder.GetDirection());
        }
    }
    
    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        if (currentPath == null || currentPath.IsEmpty())
        {
            // TODO: remove this hardcoded path to replace with something dynamic and random
            currentPath = FindShortestPath(currentPosition, Main.GetRandomPosition());
            return;
        }
        
        if (ReservedCarSpots.Count == 0)
        {
            // No spots are reserved at all, meaning the car is not yet on the road.
            AttemptToClaimTheInitialRoad(currentPath[0]);
            return;
        }
        
        var currentGoal = currentPath[0].EndPosition; 
        var direction = Math.Sign(currentGoal.Offset - currentPosition.Offset);
        var newOffset = currentPosition.Offset + direction * delta * speed;
        var newDirection = Math.Sign(currentGoal.Offset - newOffset);

        var newSpot = new ReservedCarSpot(this, newOffset, ReserveRadius, currentGoal.Road, currentPath[0].GetDirection());
        if (!CanClaimSpot(newSpot))
            return;

        if (ShouldClaimFutureSpot(currentPath))
        {
            var futureSpot = GetFutureSpot(currentPath);
            if (!CanClaimSpot(futureSpot))
                return;
            ClaimSpot(futureSpot);
        }

        ClaimSpot(newSpot);
        currentPosition.Offset = newOffset;
        Progress = (float) currentPosition.Offset;

        RemoveInvalidReservedSpots(currentPath);
        
        var isCurrentGoalReached = newDirection != direction || direction == 0;
        if (isCurrentGoalReached)
        {
            currentPath.RemoveAt(0);

            if (!currentPath.IsEmpty())
            {
                // When there is a next order, reparent to the new road and prepare for the movement.
                GetOnTheRoad(currentPath[0].StartPosition, currentPath[0].GetDirection());

                currentPosition = currentPath[0].StartPosition;
                Progress = (float)currentPosition.Offset;
            }
            
        }
        else
        {
            TakeAppropriateRoadSide(newDirection);
        }
    }

    private void RemoveInvalidReservedSpots(List<CarMovement> carMovementOrders)
    {
        var currentRoadSpot = ReservedCarSpots
            .SingleOrDefault(spot=>spot.RoadToReserve==carMovementOrders[0].GetRoad());
        var futureRoadSpot = carMovementOrders.Count <= 1 ?null:
            ReservedCarSpots
                .SingleOrDefault(spot=>spot.RoadToReserve==carMovementOrders[1].GetRoad());;
        var previousRoadSpot = ReservedCarSpots.
            SingleOrDefault(spot=> spot != currentRoadSpot && spot != futureRoadSpot);
        if (futureRoadSpot != null)
        {
            var distanceFromFutureSpot =
                Math.Abs(carMovementOrders[0].EndPosition.Offset - currentPosition.Offset);
            if (distanceFromFutureSpot > ReserveRadius)
            {
                UnclaimSpot(futureRoadSpot);
            }
        }

        if (previousRoadSpot != null)
        {
            // TODO: fix a potential bug where if a road changes every (0; ReserveRadius), this previousRoadSpot won't be removed
            var distanceFromPreviousSpot =
                Math.Abs(carMovementOrders[0].StartPosition.Offset - currentPosition.Offset);
            if (distanceFromPreviousSpot > ReserveRadius)
            {
                UnclaimSpot(previousRoadSpot);
            }
        }
        
       // GD.Print((previousRoadSpot!=null,currentRoadSpot!=null,futureRoadSpot!=null));
    }

    private bool ShouldClaimFutureSpot(List<CarMovement> carMovementOrders)
    {
        if (carMovementOrders[0].EndPosition.Road != currentPosition.Road)
        {
            GD.PushError("Unexpected behaviour! Car order list contains non-current road as the first order");
        }
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

    
    public static List<CarMovement>? FindShortestPath(Position startPosition, Position endPosition, double currentPathLength = 0, int iterations = 0,Dictionary<Position,double>? shortestDistances = null)
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
        return enumeratedPaths.IsEmpty() ? null : enumeratedPaths.MinBy(tuple => tuple.pathLength).path;
    }
}