using Godot;

namespace TrafficSimulation.scripts;

internal record RoadIntersectionExit(Vector3 WorldDirection, Road Road, int RoadDirection)
{
    public override string ToString()
    {
        return $"{Road.Name} ({RoadDirection})";
    }
}