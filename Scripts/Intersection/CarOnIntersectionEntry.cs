namespace TrafficSimulation.scripts;

internal class CarOnIntersectionEntry
{
    public Car Car;
    public Position CurrentPosition, CurrentDestination;
    public bool Entering = true;
    public ReservedCarSpot EntranceSpot, ExitSpot;
    public RoadIntersectionExit PlannedExit, Entrance;

    public CarOnIntersectionEntry(Car car, ReservedCarSpot entranceSpot, ReservedCarSpot exitSpot,
        RoadIntersectionExit entrance, RoadIntersectionExit plannedExit, Position currentPosition,
        Position currentDestination)
    {
        EntranceSpot = entranceSpot;
        ExitSpot = exitSpot;
        Car = car;
        PlannedExit = plannedExit;
        CurrentPosition = currentPosition;
        CurrentDestination = currentDestination;
        Entrance = entrance;
    }
}