using Godot;

public class TargetPractice: DamageableObj{
  [Export]
  protected int maxhealth = 100;

  private Sprite targetSprite;

  private string target_idle = "idle";
  private string target_broken = "broken";

  private int new_maxhealth = 100;

  protected override void doDamage(float dmg){
    GD.PrintErr(currenthealth);
    base.doDamage(dmg);
  }

  protected override void onHealthDepleted(){
    targetSprite.Visible = false;
    //targetSprite.Animation = target_broken;

    EmitSignal("_OnHealthDepleted");
  }

  protected override void onHealthChanged(){
    GD.PrintErr(currenthealth);
  }

  public override void _Ready(){
    base._Ready();

    targetSprite = GetNode<Sprite>("Sprite");
    changeMaxHealth(new_maxhealth);
  }
}