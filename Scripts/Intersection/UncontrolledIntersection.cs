namespace TrafficSimulation.scripts;

public class UncontrolledIntersection : RoadIntersection
{
    public UncontrolledIntersection(Road road1, double offset1, Road road2, double offset2) : base(road1, offset1,
        road2, offset2)
    {
    }
}