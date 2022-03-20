using Godot;

public class TargetPractice: DamageableObj{
  private Sprite targetSprite;

  private string target_idle = "idle";
  private string target_broken = "broken";

  private int new_maxhealth = 100;

  private void OnHealthChanged(float currhealth){
    GD.Print(currhealth, "/", maxhealth);
  }

  public override void _Ready(){
    base._Ready();

    Connect("_OnHealthChanged", this, "OnHealthChanged");

    targetSprite = GetNode<Sprite>("Sprite");
    changeMaxHealth(new_maxhealth);
  }
}