using Godot;
using System.Collections.Generic;
using gametools;

public class BackpackUI: PopupDialog{
  private VBoxContainer vbcont = new VBoxContainer();
  private Button[] buttonLists;

  public override void _Ready(){
    GetNode("ScrollContainer").AddChild(vbcont);
  }

  public void SetBackpackSize(int backpackSize){
    
  }

  public void UpdateBackpackInformation(Backpack.get_itemdatastruct[] datas){
    buttonLists = new Button[datas.Length];
    for(int i = 0; i < datas.Length; i++){
      buttonLists[i] = new Button{
        Text = string.Format("itemtype: {0},  itemid: {1}", datas[i].type.ToString(), datas[i].itemid)
      };

      vbcont.AddChild(buttonLists[i]);
    }
  }
}