using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Road : Path3D
{
    public List<(Vector3, Vector3)> lines;

    public List<Vector3> points;

    public List<ReservedCarSpot> ReservedCarSpots = new List<ReservedCarSpot>();

    // Is initialized in Main
    public List<RoadIntersection> intersectionsWithOtherRoads = new List<RoadIntersection>();
    
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        points = Curve.GetBakedPoints().Select(localPoint => localPoint + GlobalPosition).ToList();
        lines = points.Zip(points.Skip(1)).ToList();
    }

    public bool IsEnclosed()
    {
        double epsilon = 0.001;
        return Math.Abs(points[0].DistanceTo(points[^1])) <= epsilon;
    }
    
    public double PositionToOffset(Vector3 givenGlobalPosition)
    {
        return Curve.GetClosestOffset(givenGlobalPosition - GlobalPosition);
    }

    public Vector3 OffsetToPosition(double givenOffset)
    {
        return Curve.SampleBaked((float)givenOffset);
    }

    public List<RoadIntersection> GetIntersectionsWith(Road otherRoad)
    {
        if (otherRoad == this)
        {
            GD.PushError($"A road {Name} is asked to find intersections with itself!");
            return null;
        }
        return lines.Select(
            ourSegment =>
                otherRoad.lines.Select(
                    theirSegment => GetTwoSegmentIntersection(ourSegment, theirSegment)
                ).FirstOrDefault(intersection => intersection != null)
        ).NotNull().Select(intersectionPosition => new UncontrolledIntersection(
            this,
            PositionToOffset(intersectionPosition),
            otherRoad,
            otherRoad.PositionToOffset(intersectionPosition)
        ) as RoadIntersection).ToList();
    }

    private Vector3? GetTwoSegmentIntersection((Vector3, Vector3) segment1, (Vector3, Vector3) segment2)
    {
        var closestPoints = Geometry3D.GetClosestPointsBetweenSegments(segment1.Item1, segment1.Item2,
            segment2.Item1, segment2.Item2);
        var dist = closestPoints[0].DistanceTo(closestPoints[1]);
        const double epsilon = 0.001;
        return dist < epsilon ? closestPoints[0] : null;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        
    }

    public double GetMaxOffset()
    {
        return Curve.GetBakedLength();
    }
}