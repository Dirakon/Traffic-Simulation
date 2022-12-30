public record class ReservedCarSpot(Car ReservingCar, double Offset, double ReserveRadius, Road RoadToReserve, int Direction)
{
    public double GetStartingOffset() => (Offset)%RoadToReserve.Curve.GetBakedLength() - ReserveRadius;

    public double GetEndingOffset() => (Offset)%RoadToReserve.Curve.GetBakedLength() + ReserveRadius;
}