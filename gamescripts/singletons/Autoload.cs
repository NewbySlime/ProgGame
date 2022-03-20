using Godot;
using Tools;
using System.Collections.Generic;
using System.Threading.Tasks;

public static class OS_Data{
  // Windows based systems
#if GODOT_WINDOWS
  public static string OS_Based = "Windows";
  public static string ExecutableName = "exe";

  // Unix based systems
#else
  public static string OS_Based = "Unix";
  public static string ExecutableName = "bin";
#endif

}


public class Autoload: Node2D{

  [Export]
  private float CrossHairSize = 2f;

  //this uses the database from the static class of paramsdatabase
  private SocketListenerHandler listener = new SocketListenerHandler(ParamData.RegularBotParams.GetStringParam);
  private CanvasLayer ci;
  private Task currentListeningTask;
  private InputFlag currentInputFlags = new InputFlag();
  private Pointer GamePointer;

  //game ui stuff
  private BackpackUI currbui;

  //Game engine Stuffs
  private static string Infogui_Player_name = "PlayerInfoGUI";
  private static string gui_Player_name = "PlayerGUI";

  [Export]
  private StreamTexture gui_CrosshairLine_Img, gui_CrosshairDot_Img, gui_Pointer_Img;

  private static class InputContext_Flags{
    public static InputFlag Gameplay_flag = new InputFlag{
      gameplay_input = true,
      ui_input = false
    };

    public static InputFlag GUI_flag = new InputFlag{
      gameplay_input = false,
      ui_input = true
    };

    public static InputFlag NoInput_flag = new InputFlag{
      gameplay_input = false,
      ui_input = false
    };
  }

  [Export]
  private PackedScene gui_Player_Scene, Infogui_Player_Scene, gui_FileExplorer_Scene, gui_OptionsPopup_Scene, gui_ScrollAcceptDialog_Scene, gui_PlayerBackpack_scene;

  [Export]
  private PackedScene ps_smokeTrail;

  // all the game stuffs
  private Pointer.CrosshairOption GameCrosshairOption = new Pointer.CrosshairOption{
    up_line = false,
    right_line = true,
    down_line = true,
    left_line = true,
    dot = true
  };

  private Pointer.PointerOption GamePointerOption = new Pointer.PointerOption{
    isVisible = true
  };

  public InputFlag InputFlags{
    get{
      return currentInputFlags;
    }
  }

  public enum GameState{
    Resume,
    Pause
  }

  public enum InputContext{
    Gameplay,
    GUI,
    NoInput
  }

  public struct InputFlag{
    public bool ui_input;
    public bool gameplay_input;

    public int flagnum{
      get{
        return 
          (ui_input? 1: 0) |
          ((gameplay_input? 1: 0) << 1);
      }
    }
  }

  public override void _Ready(){
    GD.Randomize();
    currentListeningTask = listener.StartListening();
    GD.PrintErr("Used port: ", listener.currentport);

    ci = new CanvasLayer();
    this.AddChild(ci);

    // setting the in-game pointer
    Input.SetMouseMode(Input.MouseMode.Hidden);
    GamePointer = new Pointer();
    ci.AddChild(GamePointer);

    GameCrosshairOption.crosshairSize = CrossHairSize;

    GamePointer.LineSpriteImage = gui_CrosshairLine_Img;
    GamePointer.DotSpriteImage = gui_CrosshairDot_Img;
    GamePointer.CurrentPointerOption = GamePointerOption;
    GamePointer.CurrentCrosshairOption = GameCrosshairOption;
    //GamePointer.SetPointerContext(Pointer.PointerContext.RegularPointer);
    GamePointer.SetPointerContext(Pointer.PointerContext.Crosshair);

    gui_Player gui_p = gui_Player_Scene.Instance<gui_Player>();
    ci.AddChild(gui_p);
    gui_p.Name = gui_Player_name;

    //add backpack class
    currbui = gui_PlayerBackpack_scene.Instance<BackpackUI>();
    ci.AddChild(currbui);
  }

  public override void _Process(float delta){
    GamePointer.GlobalPosition = GetViewport().GetMousePosition();
  }

  public override void _Notification(int what){
    switch(what){
      case NotificationPredelete:{
        GD.PrintErr("Delete Autoload");
        listener.StopListening();
        break;
      }
    }
  }

  public ushort GetcurrentPort(){
    return listener.currentport;
  }

  public ref SocketListenerHandler getcurrentSocketListener(){
    return ref listener;
  }

  public gui_Player GetGui_Player(){
    return ci.GetNodeOrNull<gui_Player>(gui_Player_name);
  }

  public async Task<string> PickOptionsByPopup(string[] options){
    PopupSelection ps_scene = gui_OptionsPopup_Scene.Instance<PopupSelection>();
    ci.AddChild(ps_scene);
    string res = (await ps_scene.GetOptionsFromUser(options));
    ci.RemoveChild(ps_scene);
    return res;
  }
	
  // if extensionFilter have zero size, don't filter any
  // don't use a dot in the extension strings
	public async Task<string[]> GetFilesByFileExplorer(string[] extensionFilter){
    FileExplorer fd_scene = gui_FileExplorer_Scene.Instance<FileExplorer>();
    ci.AddChild(fd_scene);
    string[] res = (await fd_scene.GetFiles(extensionFilter));
    ci.RemoveChild(fd_scene);
    return res;
	}

  public static string[] JSON_ArrayToStrings(string array, char separator = '|'){
    string str = "";
    string[] res = new string[0];
    foreach(char c in array){
      if(c == separator){
        GD.Print(str);
        System.Array.Resize<string>(ref res, res.Length+1);
        res[res.Length-1] = str;
        str = "";
      }
      else
        str += c;
    }

    System.Array.Resize<string>(ref res, res.Length+1);
    res[res.Length-1] = str;
    return res;
  }

  public async Task PromptUserWithAcceptDialog(string title, Godot.Collections.Array message){
    ScrollAcceptDialog sad_scene = gui_ScrollAcceptDialog_Scene.Instance<ScrollAcceptDialog>();
    ci.AddChild(sad_scene);
    await sad_scene.PromptUser(title, message);
    ci.RemoveChild(sad_scene);
  }

  public void Set_InputContext(InputContext ic){
    switch(ic){
      case InputContext.Gameplay:
        currentInputFlags = InputContext_Flags.Gameplay_flag;
        break;
      
      case InputContext.GUI:
        currentInputFlags = InputContext_Flags.GUI_flag;
        break;
      
      case InputContext.NoInput:
        currentInputFlags = InputContext_Flags.NoInput_flag;
        break;
    }
  }

  public Pointer GetGamePointer(){
    return GamePointer;
  }

  public PackedScene GetSmokeTrailScene(){
    return ps_smokeTrail;
  }

  public BackpackUI GetCurrentBackpackUI(){
    return currbui;
  }
}

namespace ParamData{
  public enum RegularBotFuncCodes{
    // --- operational codes --- \\
    noreturn_code = 0x00,
    program_exit_code = 0x01,

    // --- send codes --- \\
    moveTo_code = 0x00,
    movefrontback_code = 0x01,
    turndeg_code = 0x02,

    // --- request codes --- \\
    reqpos_code = 0x00,
    reqrotdeg_code = 0x01
  }

  public class RegularBotParams{
    private static Dictionary<ushort, string> StrDictOpr = new Dictionary<ushort, string>{
      {(ushort)RegularBotFuncCodes.noreturn_code, ""},
      {(ushort)RegularBotFuncCodes.program_exit_code, ""}
    };

    private static Dictionary<ushort, string> StrDictSnd = new Dictionary<ushort, string>{
      {(ushort)RegularBotFuncCodes.moveTo_code, new string(new char[]{(char)4, (char)4})},
      {(ushort)RegularBotFuncCodes.movefrontback_code, new string(new char[]{(char)4})},
      {(ushort)RegularBotFuncCodes.turndeg_code, new string(new char[]{(char)4})}
    };

    private static Dictionary<ushort, string> StrDictReq = new Dictionary<ushort, string>{
      {(ushort)RegularBotFuncCodes.reqpos_code, ""},
      {(ushort)RegularBotFuncCodes.reqrotdeg_code, ""}
    };


    public static string GetStringParam(int templateCode, ushort functionCode){
      ref Dictionary<ushort, string> strdict = ref StrDictReq;
      bool cantfind = false;
      switch((templateCode_enum)templateCode){
        case templateCode_enum.oprCode:
          strdict = ref StrDictOpr;
          break;
        
        case templateCode_enum.sendCode:
          strdict = ref StrDictSnd;
          break;
        
        case templateCode_enum.reqCode:
          strdict = ref StrDictReq;
          break;
        
        default:
          cantfind = true;
          break;
      }

      if(!cantfind && strdict.ContainsKey(functionCode))
        return strdict[functionCode];
      else{
        GD.PrintErr("Cannot get a certain string parameter: ", templateCode, " ", functionCode);
        return "";
      }
    }
  }
}