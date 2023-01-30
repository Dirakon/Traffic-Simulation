using System;

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
}