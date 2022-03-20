using Godot;

public class testscenescript: Node2D{
  float distance = 50f;
  float angleoffset = 1f;
  Vector2 newpos, currpos;
  Vector2 Pivot = new Vector2(300f, 400f);

  public override void _Process(float delta){
    Update();
  }

  public override void _Input(InputEvent @event){
    if(@event is InputEventMouseMotion){
      // calculation here
      Vector2 mousepos = GetGlobalMousePosition();
      currpos = (mousepos - Pivot).Normalized();
      float newangle = Mathf.Atan2(currpos.y, currpos.x) - angleoffset;
      newpos = new Vector2(Mathf.Cos(newangle), Mathf.Sin(newangle));
    }
  }

  public override void _Draw(){
    DrawCircle(Pivot+(currpos*distance), 10f, Colors.Red);
    DrawCircle(Pivot+(newpos*distance), 10f, Colors.Green);
  }
}