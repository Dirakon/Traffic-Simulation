namespace TrafficSimulation.scripts;

public class TrafficLightControlledIntersection : RoadIntersection
{
    public TrafficLightControlledIntersection(Road road1, double offset1, Road road2, double offset2) : base(road1,
        offset1, road2, offset2)
    {
    }
}