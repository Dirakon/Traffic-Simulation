using Godot;
using System;

public partial class CarVisuals : CharacterBody3D
{
	private Car CarToFollow;

	private float Acceleration = 5000f,
		RotationSpeed = 20f,
		DifferenceTreshold = 0.5f,
		VelocityDissapearanceFactor = 1f,
		TurboDroppoff = 1f,
		TurboModifier = 5f;
	private float MaxSpeed = 7.5f;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void _PhysicsProcess(double delta)
	{
		var difference = CarToFollow.GlobalPosition - GlobalPosition;
		difference.Y = 0;
		
		if (difference.Length() < DifferenceTreshold)
		{
			Velocity *=VelocityDissapearanceFactor;
			return;
		}

		var direction = difference.Normalized();
		var forward = Basis.Z.Normalized();
		

		var angleDifference = forward.SignedAngleTo(direction,Vector3.Up);
		var oldSign = Math.Sign(angleDifference);

		var toRotate = oldSign * delta * RotationSpeed;
		if (Math.Abs(toRotate) > Math.Abs(angleDifference))
		{
			toRotate = angleDifference;
		}
		RotateY((float)(toRotate));

		var modifier = 1f;
		if (difference.Length() > TurboDroppoff)
		{
			modifier = (difference.Length() - TurboDroppoff) * TurboModifier;
		}
		Velocity +=forward*(float)(Acceleration*modifier*delta);

		var newMax = MaxSpeed;
		if (difference.Length() > TurboDroppoff)
		{
			newMax += (difference.Length() - TurboDroppoff) * TurboModifier;
		}

		if (Velocity.Length() > newMax)
		{
			Velocity = Velocity.Normalized() * newMax;
		}
		
		MoveAndSlide();
	}

	public void Init(Car car)
	{
		CarToFollow = car;

		var mesh = GetNode<MeshInstance3D>("Cube").Mesh;
		
		var mainMaterial = mesh.SurfaceGetMaterial(0).Duplicate(subresources: true) as StandardMaterial3D;
		mainMaterial.AlbedoColor = Color.Color8(
			(byte)Random.Shared.NextInt64(256),
			(byte)Random.Shared.NextInt64(256),
			(byte)Random.Shared.NextInt64(256),
			(byte)Random.Shared.NextInt64(256)
			);
		mesh.SurfaceSetMaterial(0,mainMaterial);
	}
}
