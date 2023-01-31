using System;
using System.Linq;

public record ReservedCarSpot(Car ReservingCar, double Offset, double ReserveRadius, Road RoadToReserve, int Direction)
{
    public double GetStartingOffset()
    {
        return RoadToReserve.IsEnclosed()
            ? Offset % RoadToReserve.Curve.GetBakedLength() - ReserveRadius
            : Math.Max(Offset - ReserveRadius, 0);
    }

    public double GetEndingOffset()
    {
        return RoadToReserve.IsEnclosed()
            ? Offset % RoadToReserve.Curve.GetBakedLength() + ReserveRadius
            : Math.Min(Offset + ReserveRadius, 0);
    }

    public Position GetPosition()
    {
        return new(Offset, RoadToReserve);
    }

    public bool CanBeClaimed()
    {
        return RoadToReserve.ReservedCarSpots.All(spot =>
            spot.Direction != Direction ||
            spot.ReservingCar == ReservingCar ||
            spot.GetStartingOffset() > GetEndingOffset() ||
            spot.GetEndingOffset() < GetStartingOffset()
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