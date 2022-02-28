using Godot;

public class DamageableObj: RigidBody2D{
  public struct DamageData{
    public float damage;
    public float damagePerTime;
    public DamageType type;
  }

  public enum DamageType{
    Normal,
    Heal,
    Toxic
  }

  [Signal]
  public delegate void _OnHealthDepleted();

  [Export]
  protected int maxhealth = 10;

  protected int maxHealth{
    get{return maxhealth;}
  }

  protected float currenthealth;


  protected void changeMaxHealth(int health, bool refillHealth = false){
    maxhealth = health;
    if(refillHealth)
      currenthealth = (float)maxhealth;
  }

  //can be reuseable for healing
  protected virtual void doDamage(float dmg){
    currenthealth -= dmg;
    onHealthChanged();
    if(currenthealth <= 0)
      onHealthDepleted();
  }

  protected virtual void onHealthChanged(){

  }

  protected virtual void onHealthDepleted(){
    
  }

  protected virtual async void DoTimeDamage(){

  }

  public override void _Ready(){
    currenthealth = (float)maxhealth;
  }

  // uses data struct for the info of the damage
  public void DoDamageToObj(DamageData dd){
    doDamage(dd.damage);
  }
}