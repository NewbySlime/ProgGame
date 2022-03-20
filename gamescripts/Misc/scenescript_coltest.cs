using Godot;

public class scenescript_coltest: Node2D{
  [Export]
  private float circleRadius = 100f;
  [Export]
  private Vector2 areapos = new Vector2(320, 680);
  private bool isInArea = false;

  Area2D area = new Area2D();
  RigidBody2D rb;

  public void OnBodyEntered(Node body){
    isInArea = true;
  }

  private void OnBodyExited(Node body){
    isInArea = false;
  }

  public override void _Ready(){
    base._Ready();

    rb = GetNode<RigidBody2D>("RigidBody2D");

    AddChild(area);
    area.Connect("body_entered", this, "OnBodyEntered");
    area.Connect("body_exited", this, "OnBodyExited");
    area.GlobalPosition = areapos;
    CircleShape2D circle = new CircleShape2D();
    circle.Radius = circleRadius;
    uint owner_id = area.CreateShapeOwner(area);
    area.ShapeOwnerAddShape(owner_id, circle);
  }

  public override void _PhysicsProcess(float delta){
    Physics2DDirectBodyState dbs = Physics2DServer.BodyGetDirectState(rb.GetRid());
    Node2D p2 = new Node2D();
    p2.Position = GetGlobalMousePosition();
    dbs.Transform = p2.Transform;
    rb._IntegrateForces(dbs);
  }

  public override void _Process(float delta){
    base._Process(delta);

    Update();
  }

  public override void _Draw(){
    base._Draw();
    DrawCircle(area.GlobalPosition, circleRadius, isInArea? Colors.Red: Colors.Green);
    DrawCircle(rb.GlobalPosition, 10f, Colors.Blue);
  }
}