using Godot;
using godcol = Godot.Collections;
using System.Collections.Generic;

//weapondata is used to save all information about the weapon
//while currentweapondata is used to save the current state of a gun in backpack
public struct weapondata{
  public struct extended_normalgundata{ 
    public int maxammo;
    // recoil max and min is based on angle the gun will shoot
    // recoil_step is the recoil added per bullet
    // recoil_cooldown is the time needed to fully recover from recoil
    public float recoil_max, recoil_min, recoil_step, recoil_cooldown, recoil_recovery;
    public float reload_time;
    // to reduce how many percent the recoil will be reduced
    public float aimdownsight_reduce;
    public int ammousage, burstfreq;
    public weaponshoottype bulletType;
    public weaponfiremode firemode;
  }

  public struct extended_throwabledata{
    //the aoemax to aoemin is relatively the same across explodeables, it will use exponential function to do so
    //time limit is used to determine how long throwable can endure until it explodes or activates
    public float aoemax, aoemin, range, timelimit;
  }

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
  public float damage, firerate;
  public float offsetpos;
  public weapontype type;
  public object extendedData;
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
        try{
          object _subdict = dict[gunname];
          if(_subdict is godcol.Dictionary){
            godcol.Dictionary subdict = (godcol.Dictionary)_subdict;
            int weaponid = (int)(float)subdict["id"];
            weapname[weaponid] = gunname;
            weapdesc[weaponid] = (string)subdict["Description"];

            weapondata wd = new weapondata{
              id = weaponid,
              damage = (float)subdict["dmg"],
              firerate = (float)subdict["firerate"],
              offsetpos = (int)(float)subdict["offsetpos"],
              type = (weapondata.weapontype)(int)(float)subdict["weapontype"]
            };

            switch(wd.type){
              case weapondata.weapontype.normal:
                wd.extendedData = new weapondata.extended_normalgundata{
                  maxammo = (int)(float)subdict["maxammo"],
                  firemode = (weaponfiremode)(int)(float)subdict["firemode"],
                  bulletType = (weaponshoottype)(int)(float)subdict["bullettype"],
                  burstfreq = (int)(float)subdict["burstbulletcount"],
                  ammousage = (int)(float)subdict["bulletuse"],
                  reload_time = (float)subdict["reload_time"],
                  
                  // recoil stuff
                  recoil_cooldown = (float)subdict["recoil_cooldown"],
                  recoil_max = (float)subdict["recoil_max"],
                  recoil_min = (float)subdict["recoil_min"],
                  recoil_recovery = (float)subdict["recoil_recovery"],
                  recoil_step = (float)subdict["recoil_step"],

                  aimdownsight_reduce = (float)subdict["aimdownsight_reduce"]
                };

                break;
            }

            weapDict.Add(
              weaponid,
              wd
            );
          }
        }
        catch(System.Exception e){
          GD.PrintErr("Cannot retrieve a value for gun '", gunname, "'.");
          GD.PrintErr("Error message:\n", e.Message);
          GD.PrintErr("\nThis weapon will not be used for the game because of lack values");
        }
      }
    }
    else{
      GD.PrintErr("weapondb.json is not dictionary.");
    }
    //adding data to weapDict
  }

  public Weapon GetNewWeapon(int weaponid){
    weapondata currentwd = weapDict[weaponid];
    Weapon weap = null;
    switch(currentwd.type){
      case weapondata.weapontype.normal:
        NormalWeapon tmp = new NormalWeapon();
        tmp.SetWeaponData(currentwd);
        weap = tmp;
        break;
    }

    return weap;
  }
}