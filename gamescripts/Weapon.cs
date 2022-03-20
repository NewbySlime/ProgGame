using Godot;
using Godot.Collections;
using Syscolg = System.Collections.Generic;
using System.Threading.Tasks;
using gametools;


// the base class has the option to customized custom stats
// but since the base class has to do the job for damaging the obj,
// the derived class has to call base class functions in order to damage something
public class Weapon: ActionObject{
  [Signal]
  protected delegate void onUpdateSignal();

  protected static string spritefr_path = "res://resources/spritefr";
  // weapon_id.tres
  protected static string spritefr_templatename = "weapon_{0}.tres";
  protected Vector2 currentAimDir = Vector2.Up;
  protected AnimatedSprite gunsprite = new AnimatedSprite();

  protected Timer fireratetimer = new Timer();
  protected weapondata thisdata = new weapondata();

  protected Autoload autoload;

  protected int AmmoCount;

  protected enum GunAnimState{
    idling,
    attacking
  }

  private static string[] AnimName = new string[]{
    "gun_idle",
    "gun_attack"
  };

  public weapondata Weapondata{
    get{
      return thisdata;
    }
  }

  public int AmmoID{
    get{
      return thisdata.itemid;
    }
  }

  public virtual int MaxAmmo{
    get{
      return -1;
    }
  }

  protected void SetAnimation(GunAnimState animState){
    gunsprite.Animation = AnimName[(int)animState];
  }

  protected virtual async Task _Reload(int ammocount){

  }

  public override void _Ready(){
    autoload = GetNode<Autoload>("/root/Autoload");

    fireratetimer.ProcessMode = Timer.TimerProcessMode.Physics;
    fireratetimer.OneShot = true;
    AddChild(fireratetimer);

    SpriteFrames sf = GD.Load<SpriteFrames>(SavefileLoader.todir(new string[]{
      spritefr_path,
      string.Format(spritefr_templatename, ((int)thisdata.id).ToString())
    }));

    gunsprite.Frames = sf;
    AddChild(gunsprite);

    gunsprite.Scale = new Vector2(3,3);
    gunsprite.FlipH = true;
    gunsprite.Centered = true;
    gunsprite.GlobalPosition = currentAimDir * thisdata.offsetpos;
    
    SetAnimation(GunAnimState.idling);
  }

  public override void _Process(float delta){
    EmitSignal("onUpdateSignal");
    //Update();
  }

  public virtual void SetWeaponData(weapondata data){
    thisdata = data;
  }

  public virtual void AimTo(Vector2 globalpoint){
    currentAimDir = (globalpoint - GlobalPosition).Normalized();
    float rotation = Mathf.Atan2(currentAimDir.y, currentAimDir.x);
    float lengthFromParent = (gunsprite.GlobalPosition-GlobalPosition).Length();
    gunsprite.GlobalPosition = GlobalPosition + (currentAimDir * lengthFromParent);
    gunsprite.GlobalRotation = rotation;

    //changes according to rotation
    if(currentAimDir.y > 0)
      gunsprite.ZIndex = 1;
    else
      gunsprite.ZIndex = 0;
    

    if(currentAimDir.x > 0)
      gunsprite.FlipV = false;
    else
      gunsprite.FlipV = true;
  }

  public void Reload(int ammocount){
    _Reload(ammocount);
  }
}


public class NormalWeapon: Weapon{
  private PackedScene ps_smokeTrail;
  private Task shoottask = null;

  private Timer recoil_cooldowntimer = new Timer();
  private Timer recoil_recoverytimer = new Timer();
  private Timer reload_timer = new Timer();

  private weapondata.extended_normalgundata extdata = new weapondata.extended_normalgundata();

  private RayCast2D rayCast = new RayCast2D();
  private bool doUpdateRecoil = false;
  private bool dofireloop = false;
  private bool isADSing = false;
  private float currentRecoil;
  // edit this
  private float rayLength = 2000;

  
  public float CurrentRecoilDegrees{
    get{
      return currentRecoil * (isADSing? extdata.aimdownsight_reduce: 1);
    }
  }

  public bool AimDownSight{
    get{
      return isADSing;
    }

    set{
      isADSing = value;
    }
  }

  public override int MaxAmmo{
    get{
      return extdata.maxammo;
    }
  }


  private void _ShootOnce(){
    int bulletcount = 1;
    if(extdata.bulletType == weaponshoottype.scatter)
      bulletcount = extdata.scatterbulletcount;

    for(int i = 0; i < bulletcount; i++){
      rayCast.GlobalPosition = GlobalPosition;
      float recoilangle = Mathf.Deg2Rad(CurrentRecoilDegrees);
      float angle = (GD.Randf() * recoilangle) + (currentAimDir.Angle() - recoilangle);
      Vector2 newAimDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
      rayCast.CastTo = (newAimDir*rayLength)+rayCast.Position;
      rayCast.ForceRaycastUpdate();
      if(rayCast.IsColliding()){
        object collidebody = rayCast.GetCollider();
              
        //then do some damage
        if(collidebody is DamageableObj)
          ((DamageableObj)collidebody).DoDamageToObj(ref thisdata.dmgdata);
      }

      effect_SmokeTrails st = ps_smokeTrail.Instance<effect_SmokeTrails>();
      autoload.AddChild(st);
      st.AddSmokeTrail(rayCast.GlobalPosition, (newAimDir*rayLength)+rayCast.GlobalPosition, 1.5f);
    } 
  }
  
  //since many threads use this, try a prevention
  private async Task shootAsync(){
    int shootfreq = 0;
    fireratetimer.WaitTime = extdata.firerate;
    switch(extdata.firemode){
      case weaponfiremode.single:
        shootfreq = 1;
        break;

      case weaponfiremode.burst:
        shootfreq = extdata.burstfreq;
        fireratetimer.WaitTime = extdata.firerate;
        break;
      
      case weaponfiremode.auto:
        shootfreq = -1;
        break;
    }

    for(uint i = 0; dofireloop && (shootfreq < 0 || i < shootfreq); i++){
      if((AmmoCount - extdata.ammousage) >= 0){
        if(fireratetimer.TimeLeft > 0)
          await ToSignal(fireratetimer, "timeout");

        if(!dofireloop)
          break;

        try{
        _ShootOnce();

        }
        catch(System.Exception e){
          GD.PrintErr("Error: ", e.Message);
        }
        AddRecoil();
        AmmoCount -= extdata.ammousage;
        fireratetimer.Start();
      }
      else{
        //a massage saying ammo is insufficient
        OnthisEvent(new warnData{
          type = ActionType.gun,
          warntype = WarnType_gun.need_reload
        });

        dofireloop = false;
      }
    }

    shoottask = null;
  }

  private void AddRecoil(){
    currentRecoil += extdata.recoil_step;
    if(currentRecoil > extdata.recoil_max)
      currentRecoil = extdata.recoil_max;
    
    doUpdateRecoil = false;
    recoil_cooldowntimer.Start(extdata.recoil_cooldown);
  }

  private void _OnCooldownTimeout(){
    recoil_recoverytimer.ProcessMode = Timer.TimerProcessMode.Physics;
    recoil_recoverytimer.Start(extdata.recoil_recovery * ((currentRecoil - extdata.recoil_min)/(extdata.recoil_max - extdata.recoil_min)));
    doUpdateRecoil = true;

    UpdateRecoilAsync();
  }

  private async void UpdateRecoilAsync(){
    while(doUpdateRecoil && recoil_recoverytimer.TimeLeft != 0){
      float deltaDegrees = extdata.recoil_max - extdata.recoil_min;
      currentRecoil = (recoil_recoverytimer.TimeLeft / extdata.recoil_recovery) * deltaDegrees + extdata.recoil_min;
      await ToSignal(this, "onUpdateSignal");
    }
  }

  // this runs the reloading animation and changing the ammocount when it finished reloading
  protected override async Task _Reload(int ammocount){
    //reload_timer.Start(extdata.reload_time);
    //await ToSignal(reload_timer, "timeout");
    AmmoCount = ammocount;
  }

  public override void _Ready(){
    base._Ready();

    recoil_cooldowntimer.OneShot = true;
    recoil_cooldowntimer.Connect("timeout", this, "_OnCooldownTimeout");
    AddChild(recoil_cooldowntimer);

    recoil_recoverytimer.OneShot = true;
    AddChild(recoil_recoverytimer);

    rayCast.CollideWithBodies = true;
    rayCast.Enabled = true;
    rayCast.AddException(GetParent());
    AddChild(rayCast);

    ps_smokeTrail = autoload.GetSmokeTrailScene();
  }

  // all values that need to be initialized with godot's nodes
  // will be initialized in the _Ready()
  // the ammocount should be initialized by the player
  // or just use a weaponstate
  public override void SetWeaponData(weapondata data){
    if(data.type == weapondata.weapontype.normal){
      base.SetWeaponData(data);
      extdata = (weapondata.extended_normalgundata)data.extendedData;
      currentRecoil = extdata.recoil_min;

      // temporary
      AmmoCount = extdata.maxammo;
    }
    else{
      GD.PrintErr("Data of weapon type is false with the current weapon class,");
      GD.PrintErr("Data of weapon type: ", data.type.ToString(), "  What type should've been: ", weapondata.weapontype.normal.ToString());
    }
  }


  // lack of cooldown timer
  public override void Action1(bool action){
    if(action){
      Shoot();
    }
    else{
      StopShoot();
    }
  }

  public override void Action2(bool action){
    isADSing = action;
  }

  public void Shoot(){
    dofireloop = true;
    if(shoottask == null)
      shoottask = shootAsync();
  }

  public void StopShoot(){
    if(shoottask != null)
      shoottask = null;
    
    dofireloop = false;
  }
}

public class Throwables: Weapon{
  private ImageTexture tmpTexture = new ImageTexture();

  private ThrowableObject throwableObject;
  private weapondata.extended_throwabledata extdata;

  private bool currentlyPreparing = false;

  
  private void OnThrowableObjectTriggered(ThrowableObject tobj){
    RemoveChild(tobj);
  }


  public override void _Ready(){
    base._Ready();

    Image img = GD.Load<StreamTexture>("res://icon.png").GetData();
    tmpTexture.CreateFromImage(img);
  }

  public override void SetWeaponData(weapondata data){
    base.SetWeaponData(data);
  }

  public override void Action1(bool isAction){
    if(isAction){
      ReadyUp();
    }
    else{
      ThrowObject();
      CookObject();
    }
  }

  public override void Action2(bool isAction){
    if(isAction){
      CookObject();
    }
  }

  public void ReadyUp(){
    OnthisEvent(new warnData{
      type = ActionType.throwable,
      warntype = WarnType_throwables.prepare_throwing
    });

    if(throwableObject == null){
      throwableObject = new ThrowableObject();
      throwableObject.SetObjectData(thisdata);
      GetNode<Autoload>("/root/Autoload").AddChild(throwableObject);
      throwableObject.SetImageTexture(tmpTexture);
      throwableObject.Connect("onTriggered", this, "OnThrowableObjectTriggered", new Array{
        throwableObject
      });
    }
    
    currentlyPreparing = true;
  }

  public void CancelThrow(){
    
  }

  public void ThrowObject(){
    OnthisEvent(new warnData{
      type = ActionType.throwable,
      warntype = WarnType_throwables.throwing
    });

    // then throw a throwable object
    throwableObject.TriggerCook();
    throwableObject.GlobalPosition = GlobalPosition;
    throwableObject.ThrowTo(GetGlobalMousePosition());
    throwableObject = null;
    currentlyPreparing = false;
  }

  public void CookObject(){
    if(currentlyPreparing)
      throwableObject.TriggerCook();
  }
}


// use throwable object
public class ProjectileWeapon: Weapon{

  public override void _Ready(){
    base._Ready();
  }

  public override void SetWeaponData(weapondata data){
    base.SetWeaponData(data);
  }

  public override void Action1(bool action){
    
  }

  public override void Action2(bool action){
    
  }

  public void Shoot(){
    
  }
}

public class MeleeWeapon: Weapon{
  [Export]
  // this will be public since it's dependant on the animation
  private float SwingDelay = 0.2f;
  private Timer SD_timer = new Timer();

  private Area2D meleeArea = new Area2D();
  private uint owner_id;
  private CircleShape2D circleArea = new CircleShape2D();

  private weapondata.extended_meleedata extdata;


  public override void _Ready(){
    base._Ready();

    AddChild(meleeArea);
    AddChild(SD_timer);
    SD_timer.OneShot = true;

    meleeArea.Monitoring = true;
    owner_id = meleeArea.CreateShapeOwner(meleeArea);
    meleeArea.ShapeOwnerAddShape(owner_id, circleArea);
  }

  public override void SetWeaponData(weapondata data){
    base.SetWeaponData(data);

    extdata = (weapondata.extended_meleedata)data.extendedData;
    circleArea.Radius = extdata.attackradius;
  }

  public override void Action1(bool action){
    if(action)
      Swing();
  }

  public override void Action2(bool action){

  }

  public override void AimTo(Vector2 globalpoint){
    base.AimTo(globalpoint);

    meleeArea.GlobalPosition = GlobalPosition + (currentAimDir * thisdata.offsetpos);
  }

  public async Task Swing(){
    SD_timer.Start(SwingDelay);
    await ToSignal(SD_timer, "timeout");

    foreach(DamageableObj dobj in meleeArea.GetOverlappingBodies())
      dobj.DoDamageToObj(ref thisdata.dmgdata);
  }
}


public class ThrowableObject: Area2D{
  // temporaries
  private float speed = 250f;


  [Signal]
  private delegate void onTriggered();
  [Signal]
  private delegate void OnPhysicsProcess();

  private Timer cook_timer = new Timer();
  private TriggerableObject trigObj;
  private Sprite throwablesSprite = new Sprite();
  
  private weapondata weapdata;
  private weapondata.extended_throwabledata.throwable_type currentType;
  private float cookTime;

  // when the area hits a body (if the throwables is a knife, or fragilenade)
  private void _OnHitBody(Node body){
    switch(currentType){
      case weapondata.extended_throwabledata.throwable_type.hazardous_nades:
      case weapondata.extended_throwabledata.throwable_type.projectiles:
        trigObj.DoTrigger();
        break;
    }
    
    EmitSignal("onTriggered");
  }

  // do damage all item 
  private void _OnCookTimeout(){
    trigObj.DoTrigger();
    EmitSignal("onTriggered");
  }

  public override void _Ready(){
    cook_timer.Connect("timeout", this, "_OnCookTimeout");
    cook_timer.OneShot = true;
    AddChild(cook_timer);
    AddChild(throwablesSprite);
    AddChild(trigObj);
    throwablesSprite.Visible = false;

    SetPhysicsProcess(false);
  }

  public override void _PhysicsProcess(float delta){
    base._PhysicsProcess(delta);

    EmitSignal("OnPhysicsProcess");
  }

  // a problem, can only set once
  public void SetObjectData(weapondata data){
    Monitoring = false;

    weapdata = data;
    weapondata.extended_throwabledata extdata = (weapondata.extended_throwabledata)data.extendedData;
    cookTime = extdata.cookTime;
    currentType = extdata.throwableType;

    switch(extdata.throwableType){
      case weapondata.extended_throwabledata.throwable_type.explosives:
        trigObj = new Explosives();
        trigObj.SetData(weapdata);
        break;
      
      case weapondata.extended_throwabledata.throwable_type.hazardous_nades:
        trigObj = new AreaHazard();
        trigObj.SetData(weapdata);
        break;
      
      default:
        Monitoring = true;
        Connect("body_entered", this, "_OnHitBody");
        break;
    }
  }

  public void TriggerCook(){
    cook_timer.Start(cookTime);
  }

  public async Task ThrowTo(Vector2 pos){
    SetPhysicsProcess(true);
    throwablesSprite.Visible = true;

    float LengthToPos = 0f;
    Vector2 DirToPos = (pos-GlobalPosition).Normalized();
    while((LengthToPos = (pos-GlobalPosition).Length()) != 0f){
      await ToSignal(this, "OnPhysicsProcess");
      Vector2 MoveTo = DirToPos;
      float deltaspeed = speed * GetPhysicsProcessDeltaTime();
      if(LengthToPos <= deltaspeed)
        MoveTo *= LengthToPos;
      else
        MoveTo *= deltaspeed;
      
      GlobalPosition += MoveTo;
    }
  }

  public void SetImageTexture(ImageTexture texture){
    throwablesSprite.Texture = texture;
  }
}

// should set the data first then add this node as child, since it is initialized in _Ready()
public class TriggerableObject: Node2D{
  private CircleShape2D circle = new CircleShape2D();
  private uint owner_Id;
  protected Area2D area = new Area2D();
  protected weapondata.extended_throwabledata extdata;
  protected damagedata dmgdata;

  public override void _Ready(){
    AddChild(area);
    area.Connect("body_entered", this, "_OnBodyEntered");
    area.Connect("body_exited", this, "_OnBodyEntered");
    owner_Id = area.CreateShapeOwner(area);
    area.ShapeOwnerAddShape(owner_Id, circle);
  }

  public virtual void DoTrigger(){

  }

  public virtual void SetData(weapondata data){
    extdata = (weapondata.extended_throwabledata)data.extendedData;
    dmgdata = data.dmgdata;

    circle.Radius = extdata.range;
  }
}

public class Explosives: TriggerableObject{
  public override void _Ready(){
    base._Ready();

    area.Monitoring = true;
  }

  public override void DoTrigger(){
    float deltaDmg = extdata.aoemax - extdata.aoemin;
    foreach(DamageableObj dobj in area.GetOverlappingBodies()){
      // do damage to damageable obj
      float range = (dobj.GlobalPosition - GlobalPosition).Length();
          
      if(range <= extdata.range){
        float rangeNormal = range / extdata.range;
        float resultDamage = deltaDmg * rangeNormal;
        // might not a good attempt
        dmgdata.damage = resultDamage;
        dobj.DoDamageToObj(ref dmgdata);
      }
      else
        GD.Print("Not Damageable");
    }
  }
}

// for lasting damage
// for example, molotovs
public class AreaHazard: TriggerableObject{
  [Signal]
  private delegate void OnPhysicsProcess();
  private Task currentTriggerTask = null;
  private Timer damageTimer = new Timer();
  private Timer lastTimer = new Timer();

  private void _OnBodyEntered(Node body){
    if(currentTriggerTask != null){
      DamageableObj dobj = body as DamageableObj;
      if(dobj != null)
        dobj.DoDamageToObj(ref dmgdata);
    }
  }

  public override void _Ready(){
    base._Ready();
    SetPhysicsProcess(false);

    AddChild(damageTimer);
    damageTimer.OneShot = true;
    AddChild(lastTimer);
    lastTimer.OneShot = true;
  }

  public override void DoTrigger(){
    // should add lasttimer's timing in the data
    lastTimer.Start();
    currentTriggerTask = _DoTrigger();
  }

  public override void _PhysicsProcess(float delta){
    EmitSignal("OnPhysicsProcess");
  }

  public async Task _DoTrigger(){
    area.Monitoring = true;

    SetPhysicsProcess(true);
    await ToSignal(this, "OnPhysicsProcess");
    SetPhysicsProcess(false);
    
    while(lastTimer.TimeLeft != 0){
      foreach(DamageableObj dobj in area.GetOverlappingBodies()){
        dobj.DoDamageToObj(ref dmgdata);
      }

      await ToSignal(damageTimer, "timeout");
    }

    currentTriggerTask = null;
  }
}