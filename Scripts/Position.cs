using System;
using Godot;

namespace TrafficSimulation.scripts;

public record struct Position(double Offset, Road Road)
{
    public override string ToString()
    {
        return Road.Name + " " + Offset;
    }

    public override int GetHashCode()
    {
        return Road.GetHashCode() + Offset.GetHashCode();
    }

    public SingleRoadPath GetShortestSingleRoadPath(Position other)
    {
        if (other.Road != Road)
        {
            GD.PushError("Trying to use GetPathOnTheSameRoad for different roads!");
            throw new InvalidOperationException();
        }

        return Road.GetShortestPath(Offset, other.Offset);
    }

    public SingleRoadPath? GetSingleRoadPathWithSetDirection(Position other, int direction)
    {
        if (other.Road != Road)
        {
            GD.PushError("Trying to use GetPathOnTheSameRoad for different roads!");
            throw new InvalidOperationException();
        }

        return Road.GetPathWithSetDirection(Offset, other.Offset, direction);
    }

    public Position? MovedBy(double offset, int direction)
    {
        return Road.MovePositionBy(this, offset, direction);
    }

    public Position MovedByCapped(double offset, int direction)
    {
        return Road.MovePositionByCapped(this, offset, direction);
    }
}