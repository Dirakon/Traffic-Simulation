using System;
using System.Collections.Generic;
using Godot;
using TrafficSimulation.scripts.extensions;

namespace TrafficSimulation.scripts;

public partial class Car : PathFollow3D
{
    [Export] public PackedScene CarVisualsPrefab;
    private List<CarMovement>? _currentPath;

    [Export] public float PositiveDirectionHOffset,
        NegativeDirectionHOffset,
        PositiveParkedHOffset,
        NegativeParkedHOffset;


    [Export] public double ReserveRadius;

    [Export] private double _speed;

    private CarState _state;

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
        _state = new CarParkedSeekingNewPath(currentPosition);
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
        if (HOffset.AlmostEqualTo(PositiveDirectionHOffset)) HOffset = PositiveParkedHOffset;
        else if (HOffset.AlmostEqualTo(NegativeDirectionHOffset)) HOffset = NegativeParkedHOffset;
        else throw new ArgumentOutOfRangeException();
    }

    private int GetParkedDirection()
    {
        if (HOffset.AlmostEqualTo(PositiveParkedHOffset)) return 1;
        if (HOffset.AlmostEqualTo(NegativeParkedHOffset)) return -1;
        throw new ArgumentOutOfRangeException();
    }

    private void UnPark()
    {
        if (HOffset.AlmostEqualTo(PositiveParkedHOffset)) HOffset = PositiveDirectionHOffset;
        else if (HOffset.AlmostEqualTo(NegativeParkedHOffset)) HOffset = NegativeDirectionHOffset;
        else throw new ArgumentOutOfRangeException();
    }

    private void TakeAppropriateRoadSide(int direction)
    {
        HOffset = direction == -1 ? NegativeDirectionHOffset : PositiveDirectionHOffset;
    }

    private bool TryFindRandomPath(Position currentPosition)
    {
        _currentPath = currentPosition.FindTheShortestPathTo(Main.GetRandomPosition(), ReserveRadius);
        return _currentPath is {Count: > 0};
    }

    private double GetMaximumCarMovement(double delta)
    {
        return delta * _speed;
    }

    private void CancelPath(ReservedCarSpot reservedSpot)
    {
        _currentPath.Clear();
        reservedSpot.UnregisterClaim();
        Park();
        _state = new CarParkedSeekingNewPath(reservedSpot.GetPosition());
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        switch (_state)
        {
            case CarParkedSeekingNewPath(var currentPosition):
                if (TryFindRandomPath(currentPosition))
                {
                    var firstOrder = _currentPath[0];
                    _state = new CarParkedToClaimLane(new ReservedCarSpot(this, firstOrder.StartPosition.Offset,
                        ReserveRadius, firstOrder.GetRoad(), firstOrder.Direction));
                }

                break;
            case CarMoving({ } reservedSpot):
                var currentOrder = _currentPath[0];
                var currentSingleRoadPath = reservedSpot.GetPosition()
                    .GetSingleRoadPathWithSetDirection(currentOrder.EndPosition, reservedSpot.Direction);
                if (currentSingleRoadPath == null)
                {
                    GD.PushWarning(
                        "Car cancels their path because the reserved spot can't make a path with a set direction");
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

                var newDistanceToGoal = movedSpot.GetPosition()
                    .GetSingleRoadPathWithSetDirection(currentOrder.EndPosition, movedSpot.Direction)!
                    .Distance;
                if (!movedSpot.CanBeClaimed()) return;
                if (newDistanceToGoal < RoadIntersection.IntersectionInteractionDistance &&
                    currentOrder.CorrelatingIntersection != null)
                {
                    var roadAfterThis = _currentPath[1].GetRoad();
                    var directionAfterThis = _currentPath[1].Direction;
                    if (currentOrder.CorrelatingIntersection.TryReservePath(this, currentOrder.GetRoad(),
                            currentOrder.Direction, roadAfterThis, directionAfterThis))
                    {
                        reservedSpot.UnregisterClaim();
                        _currentPath.RemoveAt(0);
                        _state = new CarCrossingAnIntersection(currentOrder.CorrelatingIntersection);
                        return;
                    }

                    return;
                }

                Progress = (float) movedSpot.Offset;


                if (newDistanceToGoal.AlmostEqualTo(0))
                {
                    _currentPath.RemoveAt(0);
                    reservedSpot.UnregisterClaim();
                    Park();
                    if (_currentPath.IsEmpty())
                    {
                        _state = new CarParkedSeekingNewPath(movedSpot.GetPosition());
                    }
                    else
                    {
                        var nextOrder = _currentPath[0];
                        _state = new CarParkedToClaimLane(new ReservedCarSpot(this, nextOrder.StartPosition.Offset,
                            ReserveRadius, nextOrder.GetRoad(), nextOrder.Direction));
                    }
                }
                else
                {
                    movedSpot.RegisterClaim();
                    _state = new CarMoving(movedSpot);
                }

                break;
            case CarParkedToClaimLane({ } spotToClaim):
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
                    _state = new CarMoving(spotToClaim);
                }

                break;
            case CarCrossingAnIntersection({ } intersection):
                var position = intersection.GetCurrentCarPosition(this);
                var direction = intersection.GetCurrentCarDirection(this);
                GetOnTheRoad(position, direction);
                Progress = (float) position.Offset;
                intersection.AppropriatelyMoveCar(this, GetMaximumCarMovement(delta));

                ReservedCarSpot? newSpot;
                if ((newSpot = intersection.IntersectionPassed(this)) != null)
                {
                    if (_currentPath.IsEmpty())
                    {
                        newSpot.UnregisterClaim();
                        Park();
                        _state = new CarParkedSeekingNewPath(newSpot.GetPosition());
                    }
                    else
                    {
                        _state = new CarMoving(newSpot);
                    }
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(_state));
        }
    }
}

internal abstract record CarState;

internal record CarParkedSeekingNewPath(Position CurrentPosition) : CarState;

internal record CarParkedToClaimLane(ReservedCarSpot SpotToClaim) : CarState;

internal record CarMoving(ReservedCarSpot CurrentRoadReservedSpot) : CarState;

internal record CarCrossingAnIntersection(RoadIntersection Intersection) : CarState;