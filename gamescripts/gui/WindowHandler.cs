using Godot;

public class WindowHandler: Control{
  private WindowTemplate windowContent;
  private TitleBar title;

  public override void _Ready(){
    title = // get scene
    SetChild(title);
    
  }

  public void moveWindow(Vec2 v){

  }
}


public class WindowTemplate: Control{

}
