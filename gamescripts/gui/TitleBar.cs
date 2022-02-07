using Godot;

public class TitleBar: Control{
	private Button exitButton;
  private Bool doUseWarning = false, isMouseIn = false;
  private string warningPrompt = "";
  private Popup popupWindow;

  private Vec2 OffsetToMouse, CurrentMousePos;

  [Signal]
  public delegate void on_Window_Exited();

  private void on_exitButton_clicked(){
    CloseWindow();
  }
	
	public override void _Ready(){
		exitButton = GetNode<Button>("exitButton");
    exitButton.Connect("on_clicked", this, "on_exitButton_clicked"); // check on this
    popupWindow = // get something
	}

  public override void _Process(){
    Vec2 MousePos = GetGlobalMousePosition();
    if(MousePos != CurrentMousePos)
      Position = OffsetToMouse + MousePos;
  }

  public override void _Input(InputEvent @event){
    if(@event is InputEventMouseButton){
      InputEventMouseButton ev = (InputEventMousebutton)@event;
      if(isMouseIn && ev.Button == ButtonList.){
        SetProcess = true;
        OffsetToMouse = Position - GetGlobalMousePosition();
      }else
        SetProcess = false;
    }
  }

  public async void CloseWindow(){
    if(doUseWarning){
      popupWindow.SetContentString(warningPrompt);
      popupWindow.Show();
    }

    await ToSignal(popupWindow, "wait_confirm");
    bool warn_confirm = // get what the user wants
    if(warn_confirm){
      // closing todo here
      EmitSignal("on_Window_Exited");
    }
  }

  public void UseWarning(bool bdo, string prompt){
    doUseWarning = bdo;
    warningPrompt = prompt;
  }

  public void on_mouse_entered(){
    isMouseIn = true;
  }

  public void on_mouse_exited(){
    isMouseIn = false;
  }
}
