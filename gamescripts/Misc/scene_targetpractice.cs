using Godot;

public class scene_targetpractice: Node2D{
  [Export]
  private PackedScene TargetPractice_Scene;

  [Export]
  private int HowManyTargets = 5;
  private TargetPractice[] targets;
  [Export]
  private float target_respawntime = 3;

  [Export]
  private Rect2 targetpractice_area;
  private Player playerScene;

  private void InitTargetClass(TargetPractice tp){
    AddChild(tp);
    Vector2 randomPos = new Vector2(GD.Randf() * targetpractice_area.Size.x, GD.Randf() * targetpractice_area.Size.y) + targetpractice_area.Position;
    tp.GlobalPosition = randomPos;
  }

  public override void _Ready(){
    playerScene = GetNode<Player>("Player");
    ResetAll();
  }

  public void ResetAll(){
    targets = new TargetPractice[HowManyTargets];
    for(int i = 0; i < HowManyTargets; i++){
      TargetPractice tp = TargetPractice_Scene.Instance<TargetPractice>();

      InitTargetClass(tp);

      tp.Connect("_OnHealthDepleted", this, "OnTargetDestroyed", new Godot.Collections.Array{i});
      targets[i] = tp;
    }
  }

  public async void OnTargetDestroyed(int index){
    targets[index].QueueFree();
    Timer timer = new Timer();
    AddChild(timer);
    timer.Start(target_respawntime);
    await ToSignal(timer, "timeout");
    RemoveChild(timer);
    TargetPractice newtp = TargetPractice_Scene.Instance<TargetPractice>();
    InitTargetClass(newtp);
    newtp.Connect("_OnHealthDepleted", this, "OnTargetDestroyed", new Godot.Collections.Array{index});
    targets[index] = newtp;
  }
}