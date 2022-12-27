using Godot;
using System;

public partial class Car : PathFollow3D
{
	private Position currentPosition;

	[Export()] public InEditorPosition _inEditorEndPosition;
	[Export()] private double speed;
	public Position endPosition;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.endPosition = _inEditorEndPosition.GetPosition(this);
		
		var path = GetParent() as Road;
		var offset = path.Curve.GetClosestOffset(GlobalPosition - path.GlobalPosition);
		currentPosition = new Position(path, offset);
		
		GD.Print(endPosition.road.Name);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (endPosition == null)
			return;
		
		var direction = Math.Sign(endPosition.offset - currentPosition.offset);
		currentPosition.offset +=direction* delta * speed;

		var newDirection = Math.Sign(endPosition.offset - currentPosition.offset);
		if (newDirection != direction)
		{
			endPosition = null;
		}

		Progress = (float)currentPosition.offset;
	}
}
