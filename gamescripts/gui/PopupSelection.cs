using Godot;
using System.Threading.Tasks;

public class PopupSelection: PopupDialog{
  private ScrollContainer sc_node;
  private bool isDonePicking = false;

  [Signal]
  public delegate void on_buttonPressed();
  
	public override void _Ready(){
		base._Ready();
    sc_node = GetNode<ScrollContainer>("ScrollContainer");
    Connect("popup_hide", this, "_on_cancelPicking");
	}

  public void _on_cancelPicking(){
    if(!isDonePicking)
      EmitSignal("on_buttonPressed", "");
  }

  public void _on_buttonPressed(string str){
    if(!isDonePicking)
      EmitSignal("on_buttonPressed", str);
  }

  // return empty string if the user cancel picking
  public async Task<string> GetOptionsFromUser(string[] options){
    isDonePicking = false;
    VBoxContainer vbcont = new VBoxContainer();
    sc_node.AddChild(vbcont);
    foreach(string str in options){
      Button b = new Button{
        Text = str
      };

      vbcont.AddChild(b);
      b.Connect("pressed", this, "_on_buttonPressed", new Godot.Collections.Array{str});
    }

    Popup_();
    PopupCentered();
    string res = (string)(await ToSignal(this, "on_buttonPressed"))[0];
    isDonePicking = true;
    Hide();
    sc_node.RemoveChild(vbcont);
    return res;
  }
}