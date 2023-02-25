using Godot;
using TrafficSimulation.scripts.extensions;

namespace TrafficSimulation.scripts;

public partial class InEditorPosition : Resource
{
    [Export] public double Offset;
    [Export] public NodePath Road;

    public Position GetPosition(Node node)
    {
        var actualRoad = node.GetNode<Road>(Road);
        return new Position(MathExtensions.Mod(Offset, actualRoad.GetMaxOffset()), actualRoad);
    }
}