using Godot;
using System.Threading.Tasks;

public class FileExplorer: FileDialog{
  private string[] result;
  private string[] filters;
   
  [Signal]
  public delegate void __OnFilesGet();

  public override void _Ready(){
    base._Ready();
    Connect("files_selected", this, "OnFilesSelected");
    GetCloseButton().Connect("pressed", this, "OnFileDialogClosed");
  }
	
	public void OnFileDialogClosed(){
		result = null;
		EmitSignal("__OnFilesGet");
	}

  public async void OnFilesSelected(string[] resstr){
    bool keepChecking = false;
    if(filters.Length > 0){
      foreach(string str in resstr){
        int i;
        for(i = str.Length-1; i > 0 & str[i-1] != '.'; i--)
          ;

        string currentExtension = str.Remove(0, i);
        string result = System.Array.Find<string>(filters, element => element == currentExtension);
        if(result == null){
          keepChecking = true;
          break;
        }
      }
    }

    if(keepChecking){
      await ToSignal(this, "popup_hide");
      Popup_();
      AcceptDialog cd_node = new AcceptDialog{
        WindowTitle = "",
        DialogText = "File(s) selected isn't compatible."
      };

      AddChild(cd_node);
      cd_node.WindowTitle = "";
      cd_node.PopupCentered();
      cd_node.Popup_();
      await ToSignal(cd_node, "popup_hide");
    }
    else{
      result = resstr;
      EmitSignal("__OnFilesGet");
    }
  }

  public async Task<string[]> GetFiles(string[] extensionFilter){
    System.Array.Sort<string>(extensionFilter);
    filters = extensionFilter;
    Mode = FileDialog.ModeEnum.OpenFiles;
    Access = AccessEnum.Userdata;
    CurrentDir = "user://";
    PopupCentered();
    Popup_();
    await ToSignal(this, "__OnFilesGet");
    
    for(int i = 0; i < result.Length; i++)
      result[i] = OS.GetUserDataDir() + result[i].Remove(0, 6);

    return result;
  }
}