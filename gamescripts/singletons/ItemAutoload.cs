using System.Collections.Generic;

using Godot;
using godcol = Godot.Collections;

public struct itemdata{
  public struct extended_WeaponData{

  }

  public struct extended_ArmorData{

  }

  public enum DataType{
    ammo,
    weapon,
    consumables,
    armor
  }

  public int itemid;
  public DataType type;

  // if the quantity is higher than the max in a slot, it will be parted as many stacks
  public int quantity;
  public object extendedData;
}

public class JSONdataloader: Node2D{
  protected Dictionary<int, string> itemname = new Dictionary<int, string>();
  protected Dictionary<int, string> itemdesc = new Dictionary<int, string>();

  protected string jsondataPath = "";

  protected virtual void addItemData(godcol.Dictionary itemdict, int itemid){}


  // should be called after derived class initiated
  public override void _Ready(){
    File f = new File();
    f.Open(jsondataPath, File.ModeFlags.Read);
    string jsonstore = f.GetAsText();
    
    JSONParseResult parsedobj = JSON.Parse(jsonstore);
    if(parsedobj.Result is godcol.Dictionary){
      var maindict = parsedobj.Result as godcol.Dictionary;
      foreach(string _itemname in maindict.Keys){
        try{
          object _subdict = maindict[_itemname];
          if(_subdict is godcol.Dictionary){
            var subdict = _subdict as godcol.Dictionary;
            int itemid = (int)(float)subdict["id"];
            itemname[itemid] = _itemname;
            itemdesc[itemid] = (string)subdict["Description"];

            addItemData(subdict, itemid);
          }
        }
        catch(System.Exception e){
          GD.PrintErr("Unhandled error when trying to process data.\nError msg: ", e.ToString(), "\n");
        }
      }
    }
    else{
      GD.PrintErr("File: (", jsondataPath, ") cannot be parsed to a dictionary to be processed.");
    }
  }

  public virtual string GetItemName(int itemid){
    return itemname[itemid];
  }

  public virtual string GetItemDesc(int itemid){
    return itemdesc[itemid];
  }
}


public class ItemAutoload: JSONdataloader{
  private Dictionary<int, itemdata> itemDict = new Dictionary<int, itemdata>();
  private static ItemAutoload _autoload;

  private static void setAutoloadClass(ItemAutoload ia){
    _autoload = ia;
  }

  protected override void addItemData(godcol.Dictionary itemdata, int itemid){
    try{
      GD.Print("Getting item ", itemname[itemid], " with id: ", itemid);
    }
    catch(System.Exception e){
      GD.PrintErr("Cannot retrieve a value for item '", itemname[itemid], "' of (", itemid, ").");
      GD.PrintErr("Error message:\n", e.Message, "\nStackTrace:\n", e.StackTrace);
      GD.PrintErr("\nThis item will not be included in the game because of lack values");
    }
  }

  public override void _Ready(){
    jsondataPath = "res://JSONData//item_data.json";
    base._Ready();

    setAutoloadClass(this);
  }

  public static ItemAutoload Autoload{
    get{
      return _autoload;
    }
  }
}