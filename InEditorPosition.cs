using Godot;

public partial class InEditorPosition : Resource
{
    [Export] public double offset;
    [Export] public NodePath road;

    public Position GetPosition(Node node)
    {
        var actualRode = node.GetNode<Road>(road);
        return new Position(offset % actualRode.GetMaxOffset(), actualRode);
    }
}