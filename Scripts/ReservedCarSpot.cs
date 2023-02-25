using System.Linq;

namespace TrafficSimulation.scripts;

public record ReservedCarSpot(Car ReservingCar, double Offset, double ReserveRadius, Road RoadToReserve, int Direction)
{
    public ReservedCarSpot? MovedBy(double offset, int direction)
    {
        var newPosition = GetPosition().MovedBy(offset, direction);
        if (newPosition == null)
            return null;
        return this with {Offset = newPosition.Value.Offset};
    }

    public Position GetPosition()
    {
        return new Position(Offset, RoadToReserve);
    }

    public bool CanBeClaimed()
    {
        return RoadToReserve.ReservedCarSpots.All(spot =>
            spot.Direction != Direction ||
            spot.ReservingCar == ReservingCar ||
            GetPosition().GetShortestSingleRoadPath(spot.GetPosition()).Distance > 2 * ReserveRadius
        );
    }

    public void RegisterClaim(bool removePreviousClaimFromThisCar = true)
    {
        if (removePreviousClaimFromThisCar)
        {
            var spotClaimedOnTheSameRoad =
                RoadToReserve.ReservedCarSpots.Find(potentialSpot => potentialSpot.ReservingCar == ReservingCar);
            if (spotClaimedOnTheSameRoad != null) RoadToReserve.ReservedCarSpots.Remove(spotClaimedOnTheSameRoad);
        }

        RoadToReserve.ReservedCarSpots.Add(this);
    }

    public void UnregisterClaim()
    {
        RoadToReserve.ReservedCarSpots.Remove(this);
    }
}