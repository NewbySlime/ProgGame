using Godot;

public class effect_SmokeTrails: Line2D{
  private Timer smokeTimer = new Timer();

  public override void _Ready(){
    AddChild(smokeTimer);
    smokeTimer.OneShot = true;

    Color w = Colors.White;
    w.a = 0f;
    Modulate = w;
  }

  public override void _Process(float delta){
    if(!smokeTimer.IsStopped()){
      Color c = Modulate;
      c.a = smokeTimer.TimeLeft/smokeTimer.WaitTime;
      Modulate = c;
    }
  }
  
  public void AddSmokeTrail(Vector2 from, Vector2 to, float time){
    Points = new Vector2[]{
      to, from
    };

    smokeTimer.Start(time);
  }
}