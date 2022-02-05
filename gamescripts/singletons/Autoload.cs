using Godot;
using Tools;
using System.Collections.Generic;
using System.Threading.Tasks;

public class Autoload: Node2D{
  //this uses the database from the static class of paramsdatabase
  private SocketListenerHandler listener = new SocketListenerHandler(ParamData.RegularBotParams.GetStringParam);
  private CanvasLayer ci;
  private Task currentListeningTask;

  public override void _Ready(){
    GD.Randomize();
    currentListeningTask = listener.StartListening();
    GD.PrintErr("Used port: ", listener.currentport);

    /*ci = new CanvasLayer();
    this.AddChild(ci);

    gui_Player gui_p = gui_Player_Scene.Instance<gui_Player>();
    ci.AddChild(gui_p);
    gui_p.Name = gui_Player_name;*/

    //add backpack class
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


  //Game Stuffs
  private static string gui_Player_name = "PlayerGUI";
  private static string Infogui_Player_name = "PlayerInfoGUI";

  private static string gui_Player_ResPath = "res://Scenes//Player_GUI.tscn";
  private static string Infogui_Player_ResPath = "res://Scenes//Player_InfoGUI.tscn";

  private PackedScene gui_Player_Scene = GD.Load<PackedScene>(gui_Player_ResPath);
  private PackedScene Infogui_Player_Scene = GD.Load<PackedScene>(Infogui_Player_ResPath);

  [Signal]
  public delegate void OnGamePause(game_state gs);

  public enum game_state{
    resume,
    pause
  }


  public gui_Player GetGui_Player(){
    return ci.GetNodeOrNull<gui_Player>(gui_Player_name);
  }

  public void SetGameState(game_state gs){
    EmitSignal("OnGamePause", new object[]{gs});
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