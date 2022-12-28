public record struct Position(double Offset, Road Road)
{
    public override string ToString()
    {
        return Road.Name + " " + Offset;
    }

    public override int GetHashCode()
    {
        return Road.GetHashCode() + Offset.GetHashCode();
    }
}