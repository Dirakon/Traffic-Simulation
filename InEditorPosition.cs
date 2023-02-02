using Godot;
using TrafficSimulation;

public partial class InEditorPosition : Resource
{
    [Export] public double offset;
    [Export] public NodePath road;

    public Position GetPosition(Node node)
    {
        var actualRoad = node.GetNode<Road>(road);
        return new Position( MathUtils.Mod(offset, actualRoad.GetMaxOffset()), actualRoad);
    }
}