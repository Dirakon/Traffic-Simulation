using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

public partial class Main : Node3D
{
    [Export] private Array<NodePath> inEditorRoads;
    private static Main Instance;
    private List<Road> roads;

    public static Position GetRandomPosition()
    {
        var randomRoad = Instance.roads.Random();
        return new Position(
            new Random().NextDouble() * randomRoad.GetMaxOffset(),
            randomRoad
        );
    }
    
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Instance = this;
        roads = inEditorRoads.Select(GetNode<Road>).ToList();
        roads.ForEach(road =>
        {
            road.intersectionsWithOtherRoads = roads.Where(otherRoad => otherRoad != road).SelectMany(
                road.GetIntersectionsWith
            ).ToList();
        });
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}