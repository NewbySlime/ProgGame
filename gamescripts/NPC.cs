using Godot;

public class NPC: DamageableObj{
  protected Weapon currentWeapon;
  protected AnimatedSprite entityAnim;
  protected UseableAutoload useableAutoload;
  protected Autoload gameAutoload;
  protected WeaponAutoload weapAutoload;

  public override void _Ready(){
    base._Ready();

    //temporary code
    //playerGui = GetParent().GetNode<gui_Player>("CanvasLayer/PlayerGUI");

    gameAutoload = GetNode<Autoload>("/root/Autoload");
    weapAutoload = GetNode<WeaponAutoload>("/root/WeaponAutoload");
    useableAutoload = GetNode<UseableAutoload>("/root/UseableAutoload");

    //usechecker = GetNode<Area2D>("checker");
    entityAnim = GetNode<AnimatedSprite>("PlayerSprite");
  }
}