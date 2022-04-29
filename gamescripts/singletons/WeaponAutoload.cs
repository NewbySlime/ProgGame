using Godot;
using godcol = Godot.Collections;
using System.Collections.Generic;

//weapondata is used to save all information about the weapon
//while currentweapondata is used to save the current state of a gun in backpack
public class weapondata{
  public struct extended_normalgundata{ 
    public int maxammo;
    public float firerate;
    // recoil max and min is based on angle the gun will shoot
    // recoil_step is the recoil added per bullet
    // recoil_cooldown is the time needed to fully recover from recoil
    public float recoil_max, recoil_min, recoil_step, recoil_cooldown, recoil_recovery;
    public float reload_time;
    // to reduce how many percent the recoil will be reduced
    public float aimdownsight_reduce;
    public int ammousage, burstfreq;
    public weaponshoottype bulletType;
    public int scatterbulletcount;
    public weaponfiremode firemode;
  }

  public struct extended_throwabledata{
    public enum throwable_type{
      // like nades and stuff
      explosives,
      // molotovs, potions...
      hazardous_nades,
      // like knives or tomahawk
      projectiles
    }

    //the aoemax to aoemin is relatively the same across explodeables, it will use exponential function to do so
    //time limit is used to determine how long throwable can endure until it explodes or activates
    public throwable_type throwableType;
    public float aoemax, aoemin, range;
    public float cookTime;
  }

  public struct extended_projectiledata{
    
  }

  public struct extended_meleedata{
    public float attackradius;
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
  public float offsetpos;
  public weapontype type;
  public damagedata dmgdata;
  public object extendedData;
}

public struct damagedata{
  public enum damagetype{
    normal,
    toxic,
    burn,
    eletrocute
  }

  public float damage;
  public float step;
  public float lasttime;
  public float elementalDmg;
  public bool doNormalDamage;
  public damagetype dmgtype;
}

public enum weaponshoottype{
  single,
  scatter
}

public enum weaponfiremode{
  single,
  burst,
  auto
}

public class WeaponAutoload: JSONdataloader{
  private Dictionary<int, weapondata> weapDict = new Dictionary<int, weapondata>();
  private static WeaponAutoload _autoload;

  private static void setAutoloadClass(WeaponAutoload au){
    _autoload = au;
  }

  protected override void addItemData(godcol.Dictionary subdict, int weaponid){
    try{
      weapondata wd = new weapondata{
        id = weaponid,
        offsetpos = (int)(float)subdict["offsetpos"],
        type = (weapondata.weapontype)(int)(float)subdict["weapontype"],
      };

      damagedata.damagetype currdt = (damagedata.damagetype)(int)(float)subdict["dmgtype"];
      bool doND = false;
      if(subdict.Contains("do_normal_damage"))
        doND = (float)subdict["do_normal_damage"] > 0;            

      switch(currdt){
        case damagedata.damagetype.normal:{
          wd.dmgdata = new damagedata{
            dmgtype = currdt,
            damage = (float)subdict["dmg"]
          };
          
          break;
        }
        
        case damagedata.damagetype.toxic:{
          wd.dmgdata = new damagedata{
            dmgtype = currdt,
            elementalDmg = (float)subdict["elementalDmg"],
            lasttime = (float)subdict["lasttime"],
            step = (float)subdict["step"],
            damage = doND? (float)subdict["dmg"]: 0,
            doNormalDamage = doND
          };

          break;
        }

        case damagedata.damagetype.burn:{
          wd.dmgdata = new damagedata{
            dmgtype = currdt,
            elementalDmg = (float)subdict["elementalDmg"],
            lasttime = (float)subdict["lasttime"],
            step = (float)subdict["step"],
            damage = doND? (float)subdict["dmg"]: 0,
            doNormalDamage = doND
          };

          break;
        }

        case damagedata.damagetype.eletrocute:{
          wd.dmgdata = new damagedata{
            dmgtype = currdt,
            elementalDmg = (float)subdict["elementalDmg"],
            damage = doND? (float)subdict["dmg"]: 0,
            doNormalDamage = doND
          };

          break;
        }
      }

      switch(wd.type){
        case weapondata.weapontype.normal:{
          weaponshoottype bullettype = (weaponshoottype)(int)(float)subdict["bullettype"];
          wd.extendedData = new weapondata.extended_normalgundata{
            firerate = (float)subdict["firerate"],                  
            maxammo = (int)(float)subdict["maxammo"],
            firemode = (weaponfiremode)(int)(float)subdict["firemode"],
            bulletType = bullettype,
            burstfreq = (int)(float)subdict["burstbulletcount"],
            ammousage = (int)(float)subdict["bulletuse"],
            reload_time = (float)subdict["reload_time"],
            
            // recoil stuff
            recoil_cooldown = (float)subdict["recoil_cooldown"],
            recoil_max = (float)subdict["recoil_max"],
            recoil_min = (float)subdict["recoil_min"],
            recoil_recovery = (float)subdict["recoil_recovery"],
            recoil_step = (float)subdict["recoil_step"],

            aimdownsight_reduce = (float)subdict["aimdownsight_reduce"],

            // not that needed stuff
            scatterbulletcount = (bullettype == weaponshoottype.scatter)?(int)(float)subdict["scatterbulletcount"]: 1
          };

          break;
        }

        case weapondata.weapontype.projectile_normal:
          wd.extendedData = new weapondata.extended_meleedata{

          };

          break;

        case weapondata.weapontype.throwable:
          wd.extendedData = new weapondata.extended_throwabledata{
            throwableType = (weapondata.extended_throwabledata.throwable_type)(int)(float)subdict["throwable_type"],
            aoemax = (float)subdict["aoemax"],
            aoemin = (float)subdict["aoemin"],
            range = (float)subdict["range"],
            cookTime = (float)subdict["cook_time"]
          };

          break;

        case weapondata.weapontype.melee:
            wd.extendedData = new weapondata.extended_meleedata{
              attackradius = (float)subdict["attack_radius"]
            };

          break;
      }

      weapDict.Add(
        weaponid,
        wd
      );
    }
    catch(System.Exception e){
      GD.PrintErr("Cannot retrieve a value for gun '", itemname[weaponid], "' of id (", weaponid, ").");
      GD.PrintErr("Error message:\n", e.Message, "\nStackTrace:\n", e.StackTrace);
      GD.PrintErr("\nThis weapon will not be used for the game because of lack values");
    }
  }

  public override void _Ready(){
    jsondataPath = "res://JSONData//weapon_data.json";
    base._Ready();
  }

  public Weapon GetNewWeapon(int weaponid){
    weapondata currentwd = weapDict[weaponid];
    Weapon weap = null;
    switch(currentwd.type){
      case weapondata.weapontype.normal:{
        weap = new NormalWeapon();
        weap.SetWeaponData(currentwd);
        break;
      }
      
      case weapondata.weapontype.throwable:{
        weap = new Throwables();
        weap.SetWeaponData(currentwd);
        break;
      }

      case weapondata.weapontype.projectile_normal:{
        weap = new ProjectileWeapon();
        weap.SetWeaponData(currentwd);
        break;
      }

      case weapondata.weapontype.melee:{
        weap = new MeleeWeapon();
        weap.SetWeaponData(currentwd);
        break;
      }
    }

    return weap;
  }

  public static WeaponAutoload Autoload{
    get{
      return _autoload;
    }
  }
}