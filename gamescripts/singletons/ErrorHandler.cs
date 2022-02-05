using Godot;

public class ErrorHandler: Node2D{
  private Popup popupWindow;
  
  public override void _Ready(){
    popupWindow = new Popup();
  }
  
  public void ErrorLog(string errmsg){
    GD.PrintErr(errmsg);
  }
  
  public void ErrorPopup(string errmsg){
    
  }
}
