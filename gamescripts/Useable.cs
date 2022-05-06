using Godot;

public class Useable: Node2D{
  [Export]
  private uint useableAreaSize = 100;
  private int useID;
  private UseableAutoload useableAutoload;
  private Node2D parentNode;

  public Vector2 Pos{
    get{
      return GlobalPosition;
    }
  }

  public uint UseableAreaSize{
    get{
      return useableAreaSize;
    }
  }

  ~Useable(){
    useableAutoload.RemoveUseables(useID);
  }

  public override void _Ready(){
    useableAutoload = GetNode<UseableAutoload>("/root/UseableAutoload");
    useID = useableAutoload.Add(this);
    parentNode = GetParent<Node2D>();
  }

  public virtual void OnUsed(){
    //do something
    GD.PrintErr("OnUsed() function isn't implemented");
  }
}


// in-game shop
public class GameShop: Useable{
  public override void _Ready(){
    base._Ready();
  }
  
  public override void OnUsed(){
    
  }
}

public class RechargeStation: Useable{
  public override void _Ready(){
    base._Ready();
  }
  
  public override void OnUsed(){
    // change player position to position of charging station
    // then warn player's class to change state to idle/recharging
  }
}