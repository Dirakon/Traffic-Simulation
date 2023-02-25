using System;
using Godot;

namespace TrafficSimulation.scripts;

public partial class CarVisuals : CharacterBody3D
{
    private float _acceleration = 5000f,
        _rotationSpeed = 20f,
        _differenceTreshold = 0.25f,
        _velocityDissapearanceFactor = 1f,
        _turboDroppoff = 1f,
        _turboModifier = 5f;

    private Car _carToFollow;

    private float _maxSpeed = 7.5f;

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
        var difference = _carToFollow.GlobalPosition - GlobalPosition;
        difference.Y = 0;

        if (difference.Length() < _differenceTreshold)
        {
            Velocity *= _velocityDissapearanceFactor;
            return;
        }

        var direction = difference.Normalized();
        var forward = Basis.Z.Normalized();


        var angleDifference = forward.SignedAngleTo(direction, Vector3.Up);
        var oldSign = Math.Sign(angleDifference);

        var toRotate = oldSign * delta * _rotationSpeed;
        if (Math.Abs(toRotate) > Math.Abs(angleDifference)) toRotate = angleDifference;
        RotateY((float) toRotate);

        var modifier = 1f;
        if (difference.Length() > _turboDroppoff) modifier = (difference.Length() - _turboDroppoff) * _turboModifier;
        Velocity += forward * (float) (_acceleration * modifier * delta);

        var newMax = _maxSpeed;
        if (difference.Length() > _turboDroppoff) newMax += (difference.Length() - _turboDroppoff) * _turboModifier;

        if (Velocity.Length() > newMax) Velocity = Velocity.Normalized() * newMax;

        MoveAndSlide();
    }

    public void Init(Car car)
    {
        _carToFollow = car;

        var mesh = GetNode<MeshInstance3D>("Cube").Mesh;

        var mainMaterial = mesh.SurfaceGetMaterial(0).Duplicate(true) as StandardMaterial3D;
        mainMaterial.AlbedoColor = Color.Color8(
            (byte) Random.Shared.NextInt64(256),
            (byte) Random.Shared.NextInt64(256),
            (byte) Random.Shared.NextInt64(256),
            (byte) Random.Shared.NextInt64(256)
        );
        mesh.SurfaceSetMaterial(0, mainMaterial);
    }
}