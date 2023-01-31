using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Car : PathFollow3D
{
    [Export] public PackedScene CarVisualsPrefab;
    private List<CarMovement>? currentPath;

    [Export] public float PositiveDirectionHOffset,
        NegativeDirectionHOffset,
        PositiveParkedHOffset,
        NegativeParkedHOffset;
    

    [Export] public double ReserveRadius ;

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

    private bool TryFindRandomPath(Position currentPosition)
    {
        currentPath = currentPosition.FindTheShortestPathTo(Main.GetRandomPosition(ReserveRadius), ReserveRadius);
        return currentPath is {Count: > 0};
    }

    private double GetMaximumCarMovement(double delta)
    {
        return delta * speed;
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
                    GD.Print("PATH:");
                    currentPath.ForEach(el=>GD.Print(el.ToString()));
                    var firstOrder = currentPath[0];
                    state = new CarParkedToClaimLane(new ReservedCarSpot(this, firstOrder.StartPosition.Offset,
                        ReserveRadius, firstOrder.GetRoad(), firstOrder.GetDirection()));
                }

                break;
            case CarMoving(ReservedCarSpot reservedSpot):
                var currentOrder = currentPath[0];
                var distanceToGoal = Math.Abs(reservedSpot.Offset - currentOrder.EndPosition.Offset);
                var newOffset = reservedSpot.Offset +
                                currentOrder.GetDirection() * Math.Min(GetMaximumCarMovement(delta), distanceToGoal);
                var newDistanceToGoal = Math.Abs(newOffset - currentOrder.EndPosition.Offset);
                var movedSpot = new ReservedCarSpot(this, newOffset, ReserveRadius, currentOrder.GetRoad(),
                    currentOrder.GetDirection());
                if (!movedSpot.CanBeClaimed())
                {
                    return;
                }
                if (newDistanceToGoal < ReserveRadius && currentOrder.CorrelatingIntersection != null)
                {
                    var roadAfterThis = currentPath[1].GetRoad();
                    var directionAfterThis = currentPath[1].GetDirection();
                    if (currentOrder.CorrelatingIntersection.TryReservePath(this,currentOrder.GetRoad(),
                            currentOrder.GetDirection(),roadAfterThis,directionAfterThis))
                    {
                        reservedSpot.UnregisterClaim();
                        currentPath.RemoveAt(0);
                        state = new CarCrossingAnIntersection(currentOrder.CorrelatingIntersection);
                        return;
                    }
                    else
                    {
                        return;
                    }
                }
                
                Progress = (float)movedSpot.Offset;
                
                
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
                            ReserveRadius, nextOrder.GetRoad(), nextOrder.GetDirection()));
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
                    spotToClaim.RegisterClaim();
                    GetOnTheRoad(spotToClaim.GetPosition(), spotToClaim.Direction);
                    state = new CarMoving(spotToClaim);
                }

                break;
            case CarCrossingAnIntersection(RoadIntersection intersection):
                Position position = intersection.GetCurrentCarPosition(this);
                int direction = intersection.GetCurrentCarDirection(this);
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
