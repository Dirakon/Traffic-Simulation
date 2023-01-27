public record class RoadIntersection(double OurOffset, double TheirOffset, Road OurRoad, Road OtherRoad, RoadIntersectionType IntersectionType)
{
    public Position AsOtherRoadPosition()
    {
        return new Position( TheirOffset,OtherRoad);
    }
    public Position AsOurRoadPosition()
    {
        return new Position( OurOffset,OurRoad);
    }
}


public enum RoadIntersectionType
{
    NoControl,
    TrafficLight
}