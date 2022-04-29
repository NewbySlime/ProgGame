using Godot;
using System.Collections.Generic;
using gametools;

public class BackpackUI: PopupDialog{
  private VBoxContainer vbcont = new VBoxContainer();
  private List<HBoxContainer> hbcontlists = new List<HBoxContainer>();
  private List<Control> itemSlotImgLists = new List<Control>();
  private Button[] buttonLists;

  private Texture emptySlotImg;

  public override void _Ready(){
    GetNode("ScrollContainer").AddChild(vbcont);
  }

  public void SetBackpackSize(int backpackSize, int itemPerY){
    int YSize = (int)Mathf.Floor(backpackSize/itemPerY);
    for(int iy = 0; iy < YSize; iy++){
      int currentItemCountY = iy*itemPerY;
      for(int ix = 0; ix < itemPerY && currentItemCountY+ix < backpackSize; ix++){
        
      }
    }
  }

  public void UpdateBackpackInformation(itemdata[] datas){
    buttonLists = new Button[datas.Length];
    for(int i = 0; i < datas.Length; i++){
      buttonLists[i] = new Button{
        Text = string.Format("itemtype: {0},  itemid: {1}", datas[i].type.ToString(), datas[i].itemid)
      };

      vbcont.AddChild(buttonLists[i]);
    }
  }
}
