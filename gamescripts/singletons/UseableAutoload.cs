using Godot;
using System;
using Tools.Storage;
using System.Collections.Generic;

/* This code haven't tested yet */

/**
  TEST lists:

*/

public class UseableAutoload: Node2D{
  private CustomDict<Useable> Useables = new CustomDict<Useable>();
  public struct UseableData{
    public Useable useable;
    public float length;
  }

  public int Add(Useable useableObj){
    int newid = 0;
    while(Useables.findkey(newid) >= 0){
      newid = (int)GD.Randi();
    }

    Useables.AddClass(newid, useableObj);
    return newid;
  }

  public void RemoveUseables(int id){
    Useables.Remove(id);
  }

  //change how this should be used
  public List<UseableData> GetUseables(Vector2 point, float areasize){
    List<UseableData> res = new List<UseableData>();
    GD.Print("point: ", point);
    GD.Print("Useables count: ", Useables.Length);
    for(int i = 0; i < Useables.Length; i++){
      Useable currentUseable = Useables[i];
      GD.Print("currenUseable pos: ", currentUseable.Pos);
      float useablelength = (currentUseable.Pos-point).Length();
      if(useablelength <= (currentUseable.UseableAreaSize + areasize))
        res.Add(new UseableData{
          useable = currentUseable,
          length = useablelength
        });
    }

    return res;
  }

  public object GetUseable(Vector2 point, float areasize){
    List<UseableData> resarr = GetUseables(point, areasize);
    if(resarr.Count > 0){
      int smallestIndex = 0;
      for(int i = 1; i < resarr.Count; i++){
        if(resarr[i].length < resarr[smallestIndex].length)
          smallestIndex = i;
      }
      
      return resarr[smallestIndex].useable;
    }

    return null;
  }
}