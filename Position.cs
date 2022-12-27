public record struct Position(double Offset, Road Road)
{
    public override string ToString()
    {
        return Road.Name + " " + Offset;
    }
}