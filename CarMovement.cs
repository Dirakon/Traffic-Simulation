using Godot;

public record struct CarMovement
{
    public CarMovement(Position StartPosition, Position EndPosition)
    {
        this.StartPosition = StartPosition;
        this.EndPosition = EndPosition;
        if (StartPosition.Road != EndPosition.Road)
        {
            GD.PushError($"Internal assumption that CarMovement is a path inside a single road is ruined by {this}");
        }
    }

    public override string ToString()
    {
        return "From: " +  StartPosition + '\n' +
            "To: " + EndPosition;
    }

    public Position StartPosition { get;  }
    public Position EndPosition { get;  }

    public void Deconstruct(out Position StartPosition, out Position EndPosition)
    {
        StartPosition = this.StartPosition;
        EndPosition = this.EndPosition;
    }
}