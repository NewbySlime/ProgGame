using Godot;
using Godot.Collections;
using gametools;

public class Player: NPC{
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
  
  private Vector2 dir = Vector2.Zero;
  private BackpackUI backpackGui;
  private Backpack playerBackpack = new Backpack(10);
  private Camera2D playerCamera;
  private Position2D cameraPos;
  private gui_Player playerGui;
  private bool isPlayerSprinting = false;


  private void _OpenBackpack(){
    backpackGui.Popup_();
  }

  private void ReloadWeapon(){
    int ammocount = playerBackpack.CutItems(currentWeapon.AmmoID, itemdata.DataType.ammo, currentWeapon.MaxAmmo);

    if(ammocount <= 0)
      GD.PrintErr("Ammo insufficient.");
    else
      GD.Print("Current ammo in backpack: ", playerBackpack.HowManyItems(currentWeapon.AmmoID, itemdata.DataType.ammo));
    
    currentWeapon.Reload(ammocount);
  }

  
  public override void _Ready(){
    base._Ready();

    playerCamera = GetNode<Camera2D>("CameraPos/Camera2D");
    cameraPos = GetNode<Position2D>("CameraPos");
    backpackGui = gameAutoload.GetCurrentBackpackUI();

    // temporary
    currentWeapon = weapAutoload.GetNewWeapon(0);
    AddChild(currentWeapon);

    playerBackpack.AddItem(new itemdata{
      itemid = 1,
      type = itemdata.DataType.consumables,
      quantity = 2
    });

    playerBackpack.AddItem(new itemdata{
      itemid = 0,
      type = itemdata.DataType.consumables,
      quantity = 1
    });

    playerBackpack.AddItem(new itemdata{
      itemid = 2,
      type = itemdata.DataType.consumables,
      quantity = 3
    });

    playerBackpack.AddItem(new itemdata{
      itemid = 1,
      type = itemdata.DataType.consumables,
      quantity = 10
    });

    printbpToConsole();
  }

  public override void _Process(float delta){
    switch(currentWeapon.Weapondata.type){
      case weapondata.weapontype.normal:{
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
        break;
      }

      default:{
        cameraPos.GlobalPosition = ((GetGlobalMousePosition()-GlobalPosition)* cameraOffset)+GlobalPosition;
        break;
      }
    }
  }

  /*
  protected override void onHealthChanged(){
    playerGui.Change_Health(currenthealth/maxHealth);
  }
  */


  //bug, if move buttons pressed at the same times, when some are released, the last released will not be accounted
  private byte moveleft_b = 0, moveright_b = 0, moveup_b = 0, movedown_b = 0;
  public override void _Input(InputEvent @event){
    if(@event is InputEventKey){
      if(@event.IsActionPressed("open_backpack"))
        _OpenBackpack();

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
    
      Move(dir * (isPlayerSprinting? playerSpeed_run: playerSpeed_walk));

      entityAnim.Playing = (dir != Vector2.Zero);
      if(entityAnim.Playing){
        entityAnim.Animation = "walk_anim";
        if(dir.x > 0)
          entityAnim.FlipH = false;
        else if(dir.x < 0)
          entityAnim.FlipH = true;
      }
      else
        entityAnim.Animation = "idle";

      
      if(@event.IsActionPressed("reload"))
        ReloadWeapon();

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
      currentWeapon.AimTo(GetGlobalMousePosition());
    }
  }

  public void UseObject(){
    Vector2 mousedir = (GetGlobalMousePosition()-GlobalPosition).Normalized();
    object currentuseable = useableAutoload.GetUseable(mousedir*playerUseRange+GlobalPosition, 10);
    if(currentuseable is Useable)
      ((Useable)currentuseable).OnUsed();
  }

  public void printbpToConsole(){
    itemdata?[] itemdats = playerBackpack.GetItemData();
    for(int i = 0; i < itemdats.Length; i++){
      if(!itemdats[i].HasValue)
        continue;
      
      itemdata curritem = itemdats[i].Value;
      GD.Print(string.Format("name: \"{0}\"\nid: {1}\nitem count: {2}\n\n", ItemAutoload.Autoload.GetItemName(curritem.itemid), curritem.itemid, curritem.quantity));
    }
  }
}

public class ActionObject: Node2D{
  public enum WarnType_gun{
    need_reload,
    ammo_insufficient,
    doADS
  }

  public enum WarnType_throwables{
    prepare_throwing,
    throwing
  }

  public enum ActionType{
    gun,
    throwable
  }

  public struct warnData{
    public ActionType type;
    public object warntype;
  }

  public delegate void _OnEvent(warnData data);
  public _OnEvent OnthisEvent;

  public virtual void CallInputEvent(InputEvent @event){
    
  }

  public virtual void Action1(bool action){

  }

  public virtual void Action2(bool action){

  }
}
