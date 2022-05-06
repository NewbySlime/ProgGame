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
      
      // change this, it is wrong
      foreach(string _itemname in maindict.Keys){
        try{
          switch(_itemname){
            case "item_max":{
              var subdict = maindict[_itemname];
              data_ItemMax_ammo = (int)(float)subdict["ammo"];
              data_ItemMax_weapon = (int)(float)subdict["weapon"];
              data_ItemMax_consumables = (int)(float)subdict["consumables"];
              data_ItemMax_armor = (int)(float)subdict["armor"];
              
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
    switch(type){
      case itemdata.DataType.ammo:
        return data_ItemMax_ammo;
      
      case itemdata.DataType.weapon:
        return data_ItemMax_weapon;
        
      case itemdata.DataType.consumables:
        return data_ItemMax_consumables;
      
      case itemdata.DataType.armor:
        return data_ItemMax_armor;
    }
    
    return 1;
  }

  public static WorldDefinition Autoload{
    get{
      return _autoload;
    }
  }
}
