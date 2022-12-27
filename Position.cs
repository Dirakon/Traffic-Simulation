using Godot;

public class Position
{
    public double offset;
    public Road road;

    public Position(Road road, double offset)
    {
        this.road = road;
        this.offset = offset;
    }
}