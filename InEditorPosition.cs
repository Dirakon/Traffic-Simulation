using Godot;

public partial class InEditorPosition : Resource
{
    [Export()] public double offset;
    [Export()] public NodePath road;

    public Position GetPosition(Node node)
    {
        return new Position(node.GetNode<Road>(road), offset);
    }
}