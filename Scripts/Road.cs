using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TrafficSimulation.scripts.extensions;

namespace TrafficSimulation.scripts;

public partial class Road : Path3D
{
    // Is initialized in Main
    public List<RoadIntersection> IntersectionsWithOtherRoads = new();
    private bool _isEnclosed;
    public List<(Vector3, Vector3)> Lines;

    public List<Vector3> Points;

    public List<ReservedCarSpot> ReservedCarSpots = new();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Points = Curve.GetBakedPoints().Select(localPoint => ToGlobal(localPoint)).ToList();
        Lines = Points.Zip(Points.Skip(1)).ToList();

        _isEnclosed = Points[0].DistanceTo(Points[^1]).AlmostEqualTo(0);
    }

    public SingleRoadPath GetShortestPath(double from, double to)
    {
        if (from.AlmostEqualTo(to))
            return new SingleRoadPath(this, 0, 0);
        if (IsEnclosed())
        {
            var normalPathDistance = Math.Abs(to - from);
            var loopedPathDistance = GetMaxOffset() - Math.Abs(to -
                                                               from);
            return normalPathDistance < loopedPathDistance
                ? new SingleRoadPath(this, normalPathDistance, Math.Sign(to - from))
                : new SingleRoadPath(this, loopedPathDistance, -Math.Sign(to - from));
        }

        return new SingleRoadPath(this, Math.Abs(to - from), Math.Sign(to - from));
    }

    public SingleRoadPath? GetPathWithSetDirection(double from, double to, int direction)
    {
        if (from.AlmostEqualTo(to))
            return new SingleRoadPath(this, 0, direction);
        if (!IsEnclosed())
            return direction == Math.Sign(to - from)
                ? new SingleRoadPath(this, Math.Abs(to - from), Math.Sign(to - from))
                : null;
        var normalPathDistance = Math.Abs(to - from);
        var loopedPathDistance = GetMaxOffset() - Math.Abs(to -
                                                           from);
        return direction == Math.Sign(to - from)
            ? new SingleRoadPath(this, normalPathDistance, Math.Sign(to - from))
            : new SingleRoadPath(this, loopedPathDistance, -Math.Sign(to - from));
    }


    public bool IsEnclosed()
    {
        return _isEnclosed;
    }

    public double PositionToOffset(Vector3 givenGlobalPosition)
    {
        return Curve.GetClosestOffset(ToLocal(givenGlobalPosition));
    }

    public Vector3 OffsetToPosition(double givenOffset)
    {
        return Curve.SampleBaked((float) givenOffset);
    }

    public List<RoadIntersection> GetIntersectionsWith(Road otherRoad)
    {
        if (otherRoad == this)
        {
            GD.PushError($"A road {Name} is asked to find intersections with itself!");
            throw new Exception($"A road {Name} is asked to find intersections with itself!");
        }

        return Lines.Select(
            ourSegment =>
                otherRoad.Lines.Select(
                    theirSegment => GetTwoSegmentIntersection(ourSegment, theirSegment)
                ).FirstOrDefault(intersection => intersection != null)
        ).NotNull().Select(intersectionPosition => new UncontrolledIntersection(
            this,
            PositionToOffset(intersectionPosition),
            otherRoad,
            otherRoad.PositionToOffset(intersectionPosition)
        ) as RoadIntersection).ToList();
    }

    private static Vector3? GetTwoSegmentIntersection((Vector3, Vector3) segment1, (Vector3, Vector3) segment2)
    {
        var closestPoints = Geometry3D.GetClosestPointsBetweenSegments(segment1.Item1, segment1.Item2,
            segment2.Item1, segment2.Item2);
        var dist = closestPoints[0].DistanceTo(closestPoints[1]);
        return dist.AlmostEqualTo(0) ? closestPoints[0] : null;
    }


    public double GetMaxOffset()
    {
        return Curve.GetBakedLength();
    }

    public Position? MovePositionBy(Position position, double offset, int direction)
    {
        if (_isEnclosed)
            return position with {Offset = MathExtensions.Mod(position.Offset + offset * direction, GetMaxOffset())};
        var movedPosition = position with {Offset = position.Offset + offset * direction};
        if (movedPosition.Offset < 0 || movedPosition.Offset > GetMaxOffset())
            return null;
        return movedPosition;
    }

    public Position MovePositionByCapped(Position position, double offset, int direction)
    {
        if (_isEnclosed)
            return position with {Offset = MathExtensions.Mod(position.Offset + offset * direction, GetMaxOffset())};
        return position with {Offset = Math.Clamp(0, position.Offset + offset * direction, GetMaxOffset())};
    }
}

public record SingleRoadPath(Road Road, double Distance, int Direction);