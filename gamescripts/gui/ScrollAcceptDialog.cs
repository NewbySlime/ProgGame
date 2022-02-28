using Godot;
using System.Threading.Tasks;

public class ScrollAcceptDialog: AcceptDialog{
  [Export]
  private Label l_node;
  public override void _Ready(){
    base._Ready();
    l_node = GetNode<Node>("ScrollContainer").GetNode<Node>("VBoxContainer").GetNode<Label>("Label");
  }

  public async Task PromptUser(string title, Godot.Collections.Array message){
    WindowTitle = title;
    string msg = "";
    foreach(string str in message)
      msg += str + '\n';
    
    l_node.Text = msg;
    PopupCentered();
    Popup_();
    await ToSignal(this, "popup_hide");
  }
}