using Godot;
using Godot.Collections;

public class WorldDefinition: Node2D{
  // ItemMax datas
  private int 
    data_ItemMax_ammo,
    data_ItemMax_weapon,
    data_ItemMax_consumables,
    data_ItemMax_armor;

  private static WorldDefinition _autoload;
  private static void setAutoloadClass(WorldDefinition au){
    _autoload = au;
  }
  
  public override void _Ready(){
    File f = new File();
    f.Open("res://JSONData/game_data.json", File.ModeFlags.Read);
    string jsonstore = f.GetAsText();

    JSONParseResult parsedobj = JSON.Parse(jsonstore);
    if(parsedobj.Result is Dictionary){
      var maindict = parsedobj.Result as Dictionary;
      foreach(string _itemname in maindict.Keys){
        try{
          switch(_itemname){
            case "ammo":
              break;
            
            case "weapon":
              break;
            
            case "consumables":
              break;
            
            case "armor":
              break;
          }
        }
        catch(System.Exception e){
          GD.PrintErr("Error when trying to process data.\nItem name: ", _itemname, "\nError msg: ", e.ToString(), "\n");
        }
      }
    }
  }

  public int GetItemMax(itemdata.DataType type){

  }

  public static WorldDefinition Autoload{
    get{
      return _autoload;
    }
  }
}