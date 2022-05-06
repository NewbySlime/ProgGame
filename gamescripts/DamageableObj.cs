using Godot;

public static class dmgModDefaultData{
  private static DamageableObj.dmgMod _dmgMod_One = new DamageableObj.dmgMod{
    NormalModifier = 1f,
    ToxicModifier = 1f,
    BurnModifier = 1f,
    ElectrocuteModifier = 1f,
    StunChance = 1f
  };

  private static DamageableObj.dmgMod _dmgMod_Human = new DamageableObj.dmgMod{
    NormalModifier = 1f,
    ToxicModifier = 1f,
    BurnModifier = 1f,
    ElectrocuteModifier = 1.8f,
    StunChance = 0.15f
  };

  private static DamageableObj.dmgMod _dmgMod_Robot = new DamageableObj.dmgMod{
    NormalModifier = 1.2f,
    ToxicModifier = 0f,
    BurnModifier = 1.6f,
    ElectrocuteModifier = 3.2f,
    StunChance = 0.8f
  };

  public static DamageableObj.dmgMod dmgMod_Human{
    get{
      return _dmgMod_Human;
    }
  }

  public static DamageableObj.dmgMod dmgMod_Robot{
    get{
      return _dmgMod_Robot;
    }
  }

  public static DamageableObj.dmgMod dmgMod_One{
    get{
      return _dmgMod_One;
    }
  }
}

public class DamageableObj: RigidBody2D{
  public struct dmgObjProperties{
    public struct dmgMod{
      public float NormalModifier, ToxicModifier, BurnModifier, ElectrocuteModifier;
      public float StunChance;
    }
    
    public enum ObjType{
      biological,
      metal,
      
      // other obj type will make all damage type become normal, except burn
      other
    }
    
    public dmgMod modifier;
    public ObjType Otype;
  }

  [Signal]
  private delegate void _OnHealthDepleted();
  [Signal]
  // health as float will be passed
  private delegate void _OnHealthChanged();

  // for toxic damage
  private Timer toxicDamageLastTimer = new Timer();
  private bool doToxicDamage = false;
  private float currentdamage_toxic;
  private float distanceEveryDamage, currentDistance;

  // for burn damage
  private Timer burnDamageLastTimer = new Timer();
  private Timer burnDamageStepTimer = new Timer();
  private float currentdamage_burn;

  // for electrocute damage
  private bool isStunned = false;
  private Timer StunnedTimer = new Timer();

  protected int maxhealth = 10;
  
  [Export]
  protected int actualmaxhealth = 10;

  protected dmgMod DamageModifier = dmgModDefaultData.dmgMod_One;

  protected int maxHealth{
    get{return maxhealth;}
  }
  
  protected int actualMaxHealth{
    get{return actualmaxhealth;}
  }

  protected float CurrentHealth{
    get{
      return currenthealth;
    }
  }

  private float currenthealth;

  private Timer DamageTimer = new Timer();
  private Timer LastTimer = new Timer();


  private void checkHealth(){
    EmitSignal("_OnHealthChanged", currenthealth);
    if(currenthealth <= 0)
      EmitSignal("_OnHealthDepleted");
  }

  private void OnToxicDamageFinished(){
    doToxicDamage = false;
  }

  private void OnBurnDamageFinished(){
    burnDamageStepTimer.Stop();
  }

  private void OnBurnDamageStep(){
    currenthealth -= currentdamage_burn * DamageModifier.BurnModifier;
    checkHealth();
  }

  private void OnHealthDepleted(){
    // burn damage
    burnDamageStepTimer.Stop();

    // toxic damage
    doToxicDamage = false;
  }

  // this need to be suplied just in case if the class needed it for toxic damage
  private void registerMovement(float len){
    if(doToxicDamage){
      currentDistance += len;
      if(currentDistance >= distanceEveryDamage){
        currentDistance -= distanceEveryDamage;
        currenthealth -= currentdamage_toxic * DamageModifier.ToxicModifier;
        checkHealth();
      }
    }
  }

  private void OnStunFinished(){
    isStunned = false;
  }

  private void Stunned(float time){
    isStunned = true;
    StunnedTimer.Start();
  }


  protected void changeMaxHealth(int health, bool refillHealth = false){
    maxhealth = health;
    if(refillHealth)
      currenthealth = (float)maxhealth;
  }

  //can be reuseable for healing
  protected void doDamage(float dmg){
    currenthealth -= dmg * DamageModifier.NormalModifier;
    checkHealth();
  }
  
  protected void doDamage_Toxic(ref damagedata dd){
    if(dd.doNormalDamage)
      doDamage(dd.damage);
      
    currentdamage_toxic = dd.elementalDmg;
    distanceEveryDamage = dd.step;
    toxicDamageLastTimer.Start(dd.lasttime);
    doToxicDamage = true;
    currentDistance = distanceEveryDamage;
  }

  protected void doDamage_Burn(ref damagedata dd){
    if(dd.doNormalDamage)
      doDamage(dd.damage);

    currentdamage_burn = dd.elementalDmg;
    burnDamageLastTimer.Start(dd.lasttime);
    burnDamageStepTimer.Start(dd.step);
  }

  protected void doDamage_Electrocute(ref damagedata dd){
    if(dd.doNormalDamage)
      doDamage(dd.damage);

    currenthealth -= dd.elementalDmg;
    if(GD.Randf() <= DamageModifier.StunChance)
      Stunned(dd.lasttime);
  }

  protected void Move(Vector2 Vel){
    Physics2DDirectBodyState bodyState = Physics2DServer.BodyGetDirectState(GetRid());
    bodyState.LinearVelocity = Vel;
    _IntegrateForces(bodyState);
  }


  public override void _Ready(){
    currenthealth = (float)maxhealth;

    Connect("_OnHealthDepleted", this, "OnHealthDepleted");

    AddChild(toxicDamageLastTimer);
    toxicDamageLastTimer.Connect("timeout", this, "OnToxicDamageFinished");
    toxicDamageLastTimer.OneShot = true;

    AddChild(burnDamageLastTimer);
    burnDamageLastTimer.Connect("timeout", this, "OnBurnDamageFinished");
    burnDamageLastTimer.OneShot = true;

    AddChild(burnDamageStepTimer);
    burnDamageStepTimer.Connect("timeout", this, "OnBurnDamageStep");
    burnDamageStepTimer.OneShot = false;

    AddChild(StunnedTimer);
    StunnedTimer.Connect("timeout", this, "OnStunFinished");
    StunnedTimer.OneShot = true;
  }

  
  private Vector2 lastpos = Vector2.Up;
  public override void _PhysicsProcess(float delta){
    base._PhysicsProcess(delta);

    if(GlobalPosition != lastpos){
      float dist = (GlobalPosition - lastpos).Length();
      registerMovement(dist);
      lastpos = GlobalPosition;
    }
  }

  // if the effect dealt still the same, the effect will redo itself
  // uses data struct for the info of the damage
  public void DoDamageToObj(ref damagedata dd){
    if(dd.dmgtype == damagedata.damagetype.normal || dd.doNormalDamage)
      doDamage(dd.damage);

    switch(dd.dmgtype){
      // damaging when moving
      case damagedata.damagetype.toxic:
        doDamage_Toxic(ref dd);
        break;

      // lasting damage
      case damagedata.damagetype.burn:
        doDamage_Burn(ref dd);
        break;
      
      // like normal, but it can stun people or destroy robot faster
      case damagedata.damagetype.eletrocute:
        doDamage_Electrocute(ref dd);
        break;
    }
  }
}
