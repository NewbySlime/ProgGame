using Godot;
using Godot.Collections;
using System.Threading.Tasks;
using gametools;


// the base class has the option to customized custom stats
// but since the base class has to do the job for damaging the obj,
// the derived class has to call base class functions in order to damage something
public class Weapon: ActionObject{
  [Signal]
  protected delegate void onUpdateSignal();

  private float[] weaponRotationChangeSprite = {
    Mathf.Sin(Mathf.Deg2Rad(30)),
    Mathf.Sin(Mathf.Deg2Rad(60)),
    Mathf.Sin(Mathf.Deg2Rad(90))
  };

  private string[] weaponRotationChangeSprite_strstate = {
    "gun_v",
    "gun_d",
    "gun_h"
  };

  protected static string spritefr_path = "res://resources/spritefr";
  protected static string spritefr_templatename = "weapon_{0}.tres";
  protected Vector2 currentAimDir = Vector2.Up;
  protected AnimatedSprite gunsprite = new AnimatedSprite();

  protected Timer fireratetimer = new Timer();
  protected weapondata thisdata = new weapondata();

  protected int AmmoCount;

  public override void _Ready(){
    fireratetimer.ProcessMode = Timer.TimerProcessMode.Physics;
    fireratetimer.OneShot = true;
    AddChild(fireratetimer);

    SpriteFrames sf = GD.Load<SpriteFrames>(SavefileLoader.todir(new string[]{
      spritefr_path,
      string.Format(spritefr_templatename, ((int)thisdata.type).ToString())
    }));

    gunsprite.Frames = sf;
    AddChild(gunsprite);

    gunsprite.Scale = new Vector2(3,3);
    gunsprite.FlipH = true;
    gunsprite.Centered = true;
    gunsprite.GlobalPosition = currentAimDir * thisdata.offsetpos;
  }

  public override void _Process(float delta){
    EmitSignal("onUpdateSignal");
    //Update();
  }

  public virtual void SetWeaponData(weapondata data){
    thisdata = data;
  }

  public void Aim(Vector2 globalpoint){
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
    

    float strictrot = Mathf.Abs(Mathf.Cos(rotation));
    for(int i = 0; i < 3; i++)
      if(strictrot < weaponRotationChangeSprite[i]){
        gunsprite.Animation = weaponRotationChangeSprite_strstate[i];
        break;
      }
  }
}


public class NormalWeapon: Weapon{
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


  private void _OnAction1(bool isAction){
    if(isAction){
      Shoot();
    }
    else{
      StopShoot();
    }
  }

  private void _OnAction2(bool isAction){
    isADSing = isAction;
  }

  private void _ShootOnce(){
    rayCast.GlobalPosition = GlobalPosition;
    float recoilangle = Mathf.Deg2Rad(CurrentRecoilDegrees);
    float angle = (GD.Randf() * recoilangle) + (currentAimDir.Angle() - recoilangle);
    Vector2 newAimDir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    rayCast.CastTo = (newAimDir*rayLength)+rayCast.Position;
    if(rayCast.IsColliding()){
      object collidebody = rayCast.GetCollider();
            
      //then do some damage
      if(collidebody is DamageableObj)
        ((DamageableObj)collidebody).DoDamageToObj(new DamageableObj.DamageData{damage = thisdata.damage});
    }
  }
  
  //since many threads use this, try a prevention
  private async Task shootAsync(){
    int shootfreq = 0;
    fireratetimer.WaitTime = thisdata.firerate;
    switch(extdata.firemode){
      case weaponfiremode.single:
        shootfreq = 1;
        break;

      case weaponfiremode.burst:
        shootfreq = extdata.burstfreq;
        fireratetimer.WaitTime = thisdata.firerate;
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

        _ShootOnce();
        AddRecoil();
        AmmoCount -= extdata.ammousage;
        fireratetimer.Start();
      }
      else{
        //a massage saying ammo is insufficient
        OnthisEvent(new warnData{
          type = ActionType.gun,
          warncode = (int)WarnType_gun.need_reload
        });

        dofireloop = false;
      }
    }

    shoottask = null;
  }

  // this runs the reloading animation and changing the ammocount when it finished reloading
  private async void _Reload(int ammocount){
    reload_timer.Start(extdata.reload_time);
    await ToSignal(reload_timer, "timeout");
    AmmoCount = ammocount;
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

#pragma warning disable
    UpdateRecoilAsync();
  }

  private async void UpdateRecoilAsync(){
    while(doUpdateRecoil && recoil_recoverytimer.TimeLeft != 0){
      float deltaDegrees = extdata.recoil_max - extdata.recoil_min;
      currentRecoil = (recoil_recoverytimer.TimeLeft / extdata.recoil_recovery) * deltaDegrees + extdata.recoil_min;
      await ToSignal(this, "onUpdateSignal");
    }
  }

  public override void _Ready(){
    base._Ready();

    _Action1 = _OnAction1;
    _Action2 = _OnAction2;

    recoil_cooldowntimer.OneShot = true;
    recoil_cooldowntimer.Connect("timeout", this, "_OnCooldownTimeout");
    AddChild(recoil_cooldowntimer);

    recoil_recoverytimer.OneShot = true;
    AddChild(recoil_recoverytimer);

    rayCast.CollideWithBodies = true;
    rayCast.Enabled = true;
    rayCast.AddException(GetParent());
    AddChild(rayCast);
  }

  // all values that need to be initialized with godot's nodes
  // will be initialized in the _Ready()
  // the ammocount should be initialized by the player
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

  // can exceed like how many ammo based on player backpack
  // and return how many ammo are used when reloading
  public int Reload(int ammocount){
    int ammoused = ammocount > extdata.maxammo? extdata.maxammo: ammocount;

#pragma warning disable
    _Reload(ammoused);
    return ammoused;
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

}