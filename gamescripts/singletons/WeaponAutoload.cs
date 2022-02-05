using Godot;
using godcol = Godot.Collections;
using System.Collections.Generic;

//weapondata is used to save all information about the weapon
//while currentweapondata is used to save the current state of a gun in backpack
public struct weapondata{
  public enum weapontype{
    normal,
    projectile_normal, //like rocket launcher or some sort
    throwable, //like nades and stuff. nade launcher still categorized as throwable, since it's "uses" the same mechanics
    melee
  }
  
  //itemid is ammo type id
  public int id, itemid;
  //if burstfirerate is -1, burstfirerate will be the same as firerate
  //firerate in seconds per bullet
  public float damage, firerate, burstfirerate;
  public int maxammo, ammousage, burstfreq, offsetpos;
  public weaponshoottype bulletType;
  public weaponfiremode firemode;
  public weapontype type;
  public object extendedData;
}

public struct extended_normalgundata{
}

public struct extended_throwabledata{
  //the aoemax to aoemin is relatively the same across explodeables, it will use exponential function to do so
  //time limit is used to determine how long throwable can endure until it explodes or activates
  public float aoemax, aoemin, range, timelimit;
}

public enum weaponshoottype{
  single = 0,
  scatter,
  projectile,
  throwable
}

public enum weaponfiremode{
  single = 0,
  burst,
  auto
}

public class WeaponAutoload: Node2D{
  private Dictionary<int, weapondata> weapDict = new Dictionary<int, weapondata>();
  private Dictionary<int, string> weapname = new Dictionary<int, string>();
  private Dictionary<int, string> weapdesc = new Dictionary<int, string>();
  public override void _Ready(){
    File f = new File();
    f.Open("res://JSONData//weapondb.json", File.ModeFlags.Read);
    string jsonstore = f.GetAsText();

    JSONParseResult parsedobj = JSON.Parse(jsonstore);

    if(parsedobj.Result is godcol.Dictionary){
      godcol.Dictionary dict = (godcol.Dictionary)parsedobj.Result;

      foreach(string gunname in dict.Keys){
        object _subdict = dict[gunname];
        if(_subdict is godcol.Dictionary){
          godcol.Dictionary subdict = (godcol.Dictionary)_subdict;
          int weaponid = (int)(float)subdict["id"];
          weapname[weaponid] = gunname;
          weapdesc[weaponid] = (string)subdict["Description"];
          weapDict.Add(
            weaponid,
            new weapondata{
              id = weaponid,
              damage = (float)subdict["dmg"],
              maxammo = (int)(float)subdict["maxammo"],
              firerate = (float)subdict["firerate"],
              firemode = (weaponfiremode)(int)(float)subdict["firemode"],
              bulletType = (weaponshoottype)(int)(float)subdict["bullettype"],
              burstfreq = (int)(float)subdict["burstbulletcount"],
              ammousage = (int)(float)subdict["bulletuse"],
              offsetpos = (int)(float)subdict["offsetpos"]
            }
          );
        }
      }
    }
    else{
      GD.PrintErr("weapondb.json is not dictionary.");
    }
    //adding data to weapDict
  }

  public weapondata? getWeaponDataOrNull(int id){
    weapondata? res = null;
    if(weapDict.ContainsKey(id))
      res = weapDict[id];
    else
      GD.PrintErr("Weapon isn't found, id: ", id);

    return res;
  }
}