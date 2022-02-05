using Godot;

public class DamageableObj: RigidBody2D{
  [Export]
  private int maxhealth = 10;

  protected int maxHealth{
    get{return maxhealth;}
  }

  protected float currenthealth;

  public override void _Ready(){
    currenthealth = (float)maxhealth;
  }

  //can be reuseable for healing
  public virtual void doDamage(float dmg){
    currenthealth -= dmg;
    onHealthChanged();
    if(currenthealth <= 0)
      onHealthDepleted();
  }


  protected void changeMaxHealth(int health, bool refillHealth = false){
    maxhealth = health;
    if(refillHealth)
      currenthealth = (float)maxhealth;
  }

  protected virtual void onHealthChanged(){

  }

  protected virtual void onHealthDepleted(){
    
  }
}