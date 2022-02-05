using Godot;
using Godot.Collections;
using System.Threading.Tasks;
using gametools;


public class Weapon: PlayerAction{
  [Signal]
  private delegate void onUpdateSignal();

  [Export]
  private float rayLength = 2000;
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

  private weapondata thisdata;
  private int AmmoCount;
  private Vector2 currentAimDir = new Vector2();
  private RayCast2D rayCast = new RayCast2D();
  private Timer fireratetimer = new Timer();
  private AnimatedSprite gunsprite; // = new AnimatedSprite();
  private bool dofireloop = false;

  private Task shoottask = null;
  private Task throwableTask;

  public delegate Backpack getBackpackDl();
  public getBackpackDl getBackpack;
  
  
  private void onAction1Pressed(){
    switch(thisdata.type){
      case weapondata.weapontype.normal:{
        dofireloop = true;
        if(shoottask == null)
          shoottask = shootAsync();

        break;
      }
      
      case weapondata.weapontype.throwable:{
        //cooks the throwable
        onAction2Pressed();
        break;
      }
    }
  }
  
  private void onAction1Released(){
    switch(thisdata.type){
      case weapondata.weapontype.normal:{
        if(shoottask != null)
          shoottask = null;

        dofireloop = false;
        break;
      }
      
      case weapondata.weapontype.throwable:{
        //throw the throwable
        onAction2Released();
        break;
      }  
    }    
  }
  
  private void onAction2Pressed(){
    switch(thisdata.type){
      case weapondata.weapontype.normal:{
        break;
      }

      case weapondata.weapontype.throwable:{
        f_doLoopThrow = true;
        if(throwableTask == null)
          throwableTask = ads_throwable();
        break;
      }
    }
  }
  
  private void onAction2Released(){
    switch(thisdata.type){
      case weapondata.weapontype.normal:{
        break;
      }

      case weapondata.weapontype.throwable:{
        f_doLoopThrow = false;
        if(throwableTask != null)
          throwableTask = null;

        break;
      }
    }
  }

  private async Task ads_normal(){

  }

  private bool f_doLoopThrow;
  private async Task ads_throwable(){
    while(f_doLoopThrow){
      //update the visual

      await ToSignal(this, "onUpdateSignal");
    }
  }

  //change this
  private void shoottype(){
    rayCast.GlobalPosition = GlobalPosition;
    rayCast.CastTo = (currentAimDir*rayLength)+GlobalPosition;
    if(rayCast.IsColliding()){
      object collidebody = rayCast.GetCollider();
            
      //then do some damage
      if(collidebody is DamageableObj)
        ((DamageableObj)collidebody).doDamage(thisdata.damage);
    }
  }


  //since many threads use this, try a prevention
  private async Task shootAsync(){
    int shootfreq = 0;
    fireratetimer.WaitTime = thisdata.firerate;
    switch(thisdata.firemode){
      case weaponfiremode.single:
        shootfreq = 1;
        break;

      case weaponfiremode.burst:
        shootfreq = thisdata.burstfreq;
        fireratetimer.WaitTime = thisdata.burstfirerate;
        break;
      
      case weaponfiremode.auto:
        shootfreq = -1;
        break;
    }

    for(uint i = 0; dofireloop && (shootfreq < 0 || i < shootfreq); i++){
      if((AmmoCount - thisdata.ammousage) >= 0){
        if(fireratetimer.TimeLeft > 0)
          await ToSignal(fireratetimer, "timeout");

        if(!dofireloop)
          break;

        shoottype();
        AmmoCount -= thisdata.ammousage;
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

  private void reload(){
    AmmoCount = thisdata.maxammo;
    //need backpack to get access to ammo

    Backpack playerbp = getBackpack();
    int manyammo = playerbp.CutItems(thisdata.itemid, itemdata.datatype.ammo, thisdata.maxammo-AmmoCount);

    if(manyammo > 0)
      AmmoCount += manyammo;
    else
      OnthisEvent(new warnData{
        type = ActionType.gun,
        warncode = (int)WarnType_gun.ammo_insufficient
      });
  }

  private void Aim(Vector2 globalpoint){
    currentAimDir = (globalpoint - GlobalPosition).Normalized();
    float rotation = Mathf.Atan2(currentAimDir.y, currentAimDir.x);
    gunsprite.GlobalPosition = GlobalPosition + (currentAimDir * thisdata.offsetpos);
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


  public override void _Ready(){
    fireratetimer.OneShot = true;
    rayCast.CollideWithBodies = true;
    rayCast.Enabled = true;
    rayCast.AddException(GetParent());
    AddChild(fireratetimer);
    AddChild(rayCast);

    gunsprite = GetNode<AnimatedSprite>("AnimatedSprite");
  }

  public override void _Process(float delta){
    EmitSignal("onUpdateSignal");
    //Update();
  }

  public override void _Draw(){
    /*rayCast.GlobalPosition = GlobalPosition;
    rayCast.CastTo = (currentAimDir*rayLength)+GlobalPosition;

    DrawLine(Position, currentAimDir*rayLength, (rayCast.IsColliding())? Colors.Green: Colors.Red);*/
  }

  public override void onInput(InputEvent input){
    if(input is InputEventMouseMotion iem)
      Aim(GetGlobalMousePosition());
    
    else if(input is InputEventMouseButton){
      if(input.IsActionPressed("action1"))
        onAction1Pressed();
      else if(input.IsActionReleased("action1"))
        onAction1Released();
        
      if(input.IsActionPressed("action2"))
        onAction2Pressed();
      else if(input.IsActionReleased("action2"))
        onAction2Released();

      if(input.IsActionPressed("reload"))
        reload();
    }
  }

  public void changeWeapon(int weaponID){
    WeaponAutoload currentWAuto = GetNode<WeaponAutoload>("/root/WeaponAutoload");
    weapondata? currentdata = currentWAuto.getWeaponDataOrNull(weaponID);
    if(currentdata != null){
      thisdata = (weapondata)currentdata;
      AmmoCount = thisdata.maxammo;
    }

    Aim(currentAimDir+GlobalPosition);
    
    //and some changes to visual
  }
}