using Godot;
using Godot.Collections;
using gametools;

public class Player: DamageableObj{
  [Export(PropertyHint.PlaceholderText)]
  private string useableClassName = "Useable";
  [Export]
  private uint playerSpeed_run = 400;
  [Export]
  private uint playerSpeed_walk = 200;
  [Export]
  private float playerUseRange = 20;
  [Export]
  private float playerUseArea = 10;
  [Export]
  private float cameraOffset = 0.2f;
  [Export]
  private float cameraOffset_ADS = 0.5f;
  
  private Weapon currentWeapon;
  private Vector2 dir = Vector2.Zero;
  private AnimatedSprite playerAnim;
  private UseableAutoload useableAutoload;
  private Autoload gameAutoload;
  private WeaponAutoload weapAutoload;
  private Backpack playerBackpack = new Backpack(10);
  private Camera2D playerCamera;
  private Position2D cameraPos;
  private gui_Player playerGui;
  private bool isPlayerSprinting = false;

  
  public override void _Ready(){
    base._Ready();
    //temporary code
    //playerGui = GetParent().GetNode<gui_Player>("CanvasLayer/PlayerGUI");

    gameAutoload = GetNode<Autoload>("/root/Autoload");
    weapAutoload = GetNode<WeaponAutoload>("/root/WeaponAutoload");

    //usechecker = GetNode<Area2D>("checker");
    useableAutoload = GetNode<UseableAutoload>("/root/UseableAutoload");

    playerCamera = GetNode<Camera2D>("CameraPos/Camera2D");
    cameraPos = GetNode<Position2D>("CameraPos");

    playerAnim = GetNode<AnimatedSprite>("PlayerSprite");
    // temporary
    currentWeapon = weapAutoload.GetNewWeapon(0);
    currentWeapon.OnthisEvent = OnActionWarn;
    AddChild(currentWeapon);
  }

  public override void _Process(float delta){
    NormalWeapon nw = (NormalWeapon)currentWeapon;

    // update crosshair
    Pointer currentPointer = gameAutoload.GetGamePointer();
    float currentRecoilAngle = nw.CurrentRecoilDegrees;
    float mouseLengthToPlayer = (GetGlobalMousePosition() - GlobalPosition).Length();
    float diameterLength =
      Mathf.Sin(Mathf.Deg2Rad(currentRecoilAngle)) *
      mouseLengthToPlayer /
      Mathf.Sin(Mathf.Deg2Rad((180-currentRecoilAngle)/2));

    currentPointer.SetDiameter(diameterLength);
    cameraPos.GlobalPosition = ((GetGlobalMousePosition()-GlobalPosition)* (nw.AimDownSight? cameraOffset_ADS: cameraOffset))+GlobalPosition;
  }

  protected override void onHealthChanged(){
    playerGui.Change_Health(currenthealth/maxHealth);
  }


  //bug, if move buttons pressed at the same times, when some are released, the last released will not be accounted
  private byte moveleft_b = 0, moveright_b = 0, moveup_b = 0, movedown_b = 0;
  public override void _Input(InputEvent @event){
    if(@event is InputEventKey){
      if(@event.IsActionPressed("move_up"))
        moveup_b = 1;
      else if(@event.IsActionReleased("move_up"))
        moveup_b = 0;

      if(@event.IsActionPressed("move_down"))
        movedown_b = 1;
      else if(@event.IsActionReleased("move_down"))
        movedown_b = 0;
      
      if(@event.IsActionPressed("move_left"))
        moveleft_b = 1;
      else if(@event.IsActionReleased("move_left"))
        moveleft_b = 0;

      if(@event.IsActionPressed("move_right"))
        moveright_b = 1;
      else if(@event.IsActionReleased("move_right"))
        moveright_b = 0;

      if(@event.IsActionPressed("sprint_key"))
        isPlayerSprinting = true;
      else if(@event.IsActionReleased("sprint_key"))
        isPlayerSprinting = false;

      dir = new Vector2(
        (moveright_b * 1) + (moveleft_b  * -1),
        (moveup_b * -1) + (movedown_b * 1)
      );
      
      Physics2DDirectBodyState bodyState = Physics2DServer.BodyGetDirectState(GetRid());
      bodyState.LinearVelocity = dir * (isPlayerSprinting? playerSpeed_run: playerSpeed_walk);
      _IntegrateForces(bodyState);

      playerAnim.Playing = (dir != Vector2.Zero);
      if(playerAnim.Playing){
        playerAnim.Animation = "walk_anim";
        if(dir.x > 0)
          playerAnim.FlipH = false;
        else if(dir.x < 0)
          playerAnim.FlipH = true;
      }
      else
        playerAnim.Animation = "idle";  

      if(@event.IsActionPressed("use_object"))
        UseObject();
    }
    else if(@event is InputEventMouseButton){
      if(@event.IsActionPressed("action1")){
        currentWeapon.Action1(true);
      }
      else if(@event.IsActionReleased("action1")){
        currentWeapon.Action1(false);
      }

      if(@event.IsActionPressed("action2")){
        currentWeapon.Action2(true);
      }
      else if(@event.IsActionReleased("action2")){
        currentWeapon.Action2(false);
      }
    }
    else if(@event is InputEventMouseMotion){
      currentWeapon.Aim(GetGlobalMousePosition());
    }
  }


  public void OnActionWarn(ActionObject.warnData warn){
    
  }

  public void UseObject(){
    Vector2 mousedir = (GetGlobalMousePosition()-GlobalPosition).Normalized();
    object currentuseable = useableAutoload.GetUseable(mousedir*playerUseRange+GlobalPosition, 10);
    if(currentuseable is Useable)
      ((Useable)currentuseable).OnUsed();
  }
}

public class ActionObject: Node2D{
  public enum WarnType_gun{
    need_reload,
    ammo_insufficient,
    doADS
  }

  public enum ActionType{
    gun
  }

  public struct warnData{
    public ActionType type;
    public int warncode;
  }


  protected _ActionDelegate _Action1, _Action2;

  public delegate void _OnEvent(warnData data);
  // isAction is like input pressed and released
  public delegate void _ActionDelegate(bool isAction);
  public _OnEvent OnthisEvent;

  public _ActionDelegate Action1{
    get{
      return _Action1;
    }
  }

  public _ActionDelegate Action2{
    get{
      return _Action2;
    }
  }
}