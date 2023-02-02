using System;
using System.Collections.Generic;
using Godot;

public partial class Car : PathFollow3D
{
    [Export] public PackedScene CarVisualsPrefab;
    private List<CarMovement>? currentPath;

    [Export] public float PositiveDirectionHOffset,
        NegativeDirectionHOffset,
        PositiveParkedHOffset,
        NegativeParkedHOffset;


    [Export] public double ReserveRadius;

    [Export] private double speed;

    private CarState state;

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
        var currentPosition = new Position(offset, road);
        state = new CarParkedSeekingNewPath(currentPosition);
        GetOnTheRoad(currentPosition, 1);
        Park();
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
    }

    private void Park()
    {
        const double epsilon = 0.0001;
        if (Math.Abs(HOffset - PositiveDirectionHOffset) < epsilon) HOffset = PositiveParkedHOffset;
        else if (Math.Abs(HOffset - NegativeDirectionHOffset) < epsilon) HOffset = NegativeParkedHOffset;
        else throw new ArgumentOutOfRangeException();
    }

    private int GetParkedDirection()
    {
        const double epsilon = 0.0001;
        if (Math.Abs(HOffset - PositiveParkedHOffset) < epsilon) return 1;
        if (Math.Abs(HOffset - NegativeParkedHOffset) < epsilon) return -1;
        throw new ArgumentOutOfRangeException();
    }

    private void UnPark()
    {
        const double epsilon = 0.0001;
        if (Math.Abs(HOffset - PositiveParkedHOffset) < epsilon) HOffset = PositiveDirectionHOffset;
        else if (Math.Abs(HOffset - NegativeParkedHOffset) < epsilon) HOffset = NegativeDirectionHOffset;
        else throw new ArgumentOutOfRangeException();
    }

    private void TakeAppropriateRoadSide(int direction)
    {
        HOffset = direction == -1 ? NegativeDirectionHOffset : PositiveDirectionHOffset;
    }

    private bool TryFindRandomPath(Position currentPosition)
    {
        currentPath = currentPosition.FindTheShortestPathTo(Main.GetRandomPosition(), ReserveRadius);
        return currentPath is {Count: > 0};
    }

    private double GetMaximumCarMovement(double delta)
    {
        return delta * speed;
    }

    private void CancelPath(ReservedCarSpot reservedSpot)
    {
        currentPath.Clear();
        reservedSpot.UnregisterClaim();
        Park();
        state = new CarParkedSeekingNewPath(reservedSpot.GetPosition());
    }
    
    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        var epsilon = 0.001;
        switch (state)
        {
            case CarParkedSeekingNewPath(Position currentPosition):
                if (TryFindRandomPath(currentPosition))
                {
                    var firstOrder = currentPath[0];
                    state = new CarParkedToClaimLane(new ReservedCarSpot(this, firstOrder.StartPosition.Offset,
                        ReserveRadius, firstOrder.GetRoad(), firstOrder.Direction));
                }

                break;
            case CarMoving(ReservedCarSpot reservedSpot):
                var currentOrder = currentPath[0];
                var currentSingleRoadPath = reservedSpot.GetPosition().GetSingleRoadPathWithSetDirection(currentOrder.EndPosition,reservedSpot.Direction);
                if (currentSingleRoadPath == null)
                {
                    GD.PushWarning("Car cancels their path because the reserved spot can't make a path with a set direction");
                    CancelPath(reservedSpot);
                    return;
                }

                var distanceToGoal = currentSingleRoadPath.Distance;
                var movedSpot = reservedSpot.MovedBy(Math.Min(GetMaximumCarMovement(delta), distanceToGoal),
                    reservedSpot.Direction);
                if (movedSpot == null)
                {
                    GD.PushWarning("Car cancels the spot moves out of the enclosed road's bounds");
                    CancelPath(reservedSpot);
                    return;
                }

                var newDistanceToGoal = movedSpot.GetPosition().GetSingleRoadPathWithSetDirection(currentOrder.EndPosition,movedSpot.Direction)!
                    .Distance;
                if (!movedSpot.CanBeClaimed()) return;
                if (newDistanceToGoal < RoadIntersection.IntersectionInteractionDistance &&
                    currentOrder.CorrelatingIntersection != null)
                {
                    var roadAfterThis = currentPath[1].GetRoad();
                    var directionAfterThis = currentPath[1].Direction;
                    if (currentOrder.CorrelatingIntersection.TryReservePath(this, currentOrder.GetRoad(),
                            currentOrder.Direction, roadAfterThis, directionAfterThis))
                    {
                        reservedSpot.UnregisterClaim();
                        currentPath.RemoveAt(0);
                        state = new CarCrossingAnIntersection(currentOrder.CorrelatingIntersection);
                        return;
                    }

                    return;
                }

                Progress = (float) movedSpot.Offset;


                if (newDistanceToGoal < epsilon)
                {
                    currentPath.RemoveAt(0);
                    reservedSpot.UnregisterClaim();
                    Park();
                    if (currentPath.IsEmpty())
                    {
                        state = new CarParkedSeekingNewPath(movedSpot.GetPosition());
                    }
                    else
                    {
                        var nextOrder = currentPath[0];
                        state = new CarParkedToClaimLane(new ReservedCarSpot(this, nextOrder.StartPosition.Offset,
                            ReserveRadius, nextOrder.GetRoad(), nextOrder.Direction));
                    }
                }
                else
                {
                    movedSpot.RegisterClaim();
                    state = new CarMoving(movedSpot);
                }

                break;
            case CarParkedToClaimLane(ReservedCarSpot spotToClaim):
                if (spotToClaim.CanBeClaimed())
                {
                    var currentDirection = GetParkedDirection();
                    if (currentDirection != spotToClaim.Direction)
                    {
                        // We need to pass through the other lane to reach there
                        var otherLaneSpot = spotToClaim with {Direction = currentDirection};
                        if (!otherLaneSpot.CanBeClaimed())
                            return;
                    }

                    spotToClaim.RegisterClaim();
                    GetOnTheRoad(spotToClaim.GetPosition(), spotToClaim.Direction);
                    state = new CarMoving(spotToClaim);
                }

                break;
            case CarCrossingAnIntersection(RoadIntersection intersection):
                var position = intersection.GetCurrentCarPosition(this);
                var direction = intersection.GetCurrentCarDirection(this);
                GetOnTheRoad(position, direction);
                Progress = (float) position.Offset;
                intersection.AppropriatelyMoveCar(this, GetMaximumCarMovement(delta));

                ReservedCarSpot? newSpot;
                if ((newSpot = intersection.IntersectionPassed(this)) != null)
                {
                    if (currentPath.IsEmpty())
                    {
                        newSpot.UnregisterClaim();
                        Park();
                        state = new CarParkedSeekingNewPath(newSpot.GetPosition());
                    }
                    else
                    {
                        state = new CarMoving(newSpot);
                    }
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state));
        }
    }
}

internal abstract record CarState;

internal record CarParkedSeekingNewPath(Position CurrentPosition) : CarState;

internal record CarParkedToClaimLane(ReservedCarSpot SpotToClaim) : CarState;

internal record CarMoving(ReservedCarSpot CurrentRoadReservedSpot) : CarState;

internal record CarCrossingAnIntersection(RoadIntersection Intersection) : CarState;