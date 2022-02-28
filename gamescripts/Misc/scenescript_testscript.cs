using Godot;
using System;

public class scenescript_testscript : Node2D{
   private BotCore bot1, bot2;
   private Button confirmbutton, confirmbutton2;
   private ScriptLoader sloader;
   private Label textbox;

   private bool onFocus = false, isMouseOnTop = false;
   private int startTextLength = 0;
   
   public override void _Ready(){
      sloader = GetNode<ScriptLoader>("/root/ScriptLoader");
      bot1 = GetNode<BotCore>("Bot");
      bot2 = GetNode<BotCore>("Bot2");
      confirmbutton2 = GetNode<Button>("Button2");
      confirmbutton2.Connect("pressed", this, "on_buttonPressed2");
      confirmbutton = GetNode<Button>("Button");
      confirmbutton.Connect("pressed", this, "on_buttonPressed");
      textbox = GetNode<Label>("Textbox");
      textbox.Connect("mouse_entered", this, "on_mouseEntered");
      textbox.Connect("mouse_exited", this, "on_mouseExited");
      startTextLength = textbox.Text.Length;

      GetNode<SavefileLoader>("/root/SavefileLoader").addUser("testuser");
      sloader.BindUsername("testuser");
   }

   public override void _Input(InputEvent @event){
      if(@event is InputEventMouseButton){
         InputEventMouseButton key = (InputEventMouseButton)@event;
         if(key.Pressed){
            switch((ButtonList)key.ButtonIndex){
               case ButtonList.Left:{
                  onFocus = isMouseOnTop;
                  break;
               }
            }
         }
      }
      else if(onFocus && @event is InputEventKey){
         InputEventKey key = (InputEventKey)@event;
         uint keycode = (uint)key.GetScancodeWithModifiers();
         if(key.Pressed){
            switch((KeyList)keycode){
               case KeyList.Backspace:{
                  if(textbox.Text.Length >= startTextLength)
                     textbox.Text = textbox.Text.Remove(textbox.Text.Length-1, 1);

                  break;
               }

               case KeyList.Enter:{
                  onFocus = false;
                  break;
               }

               default:{
                  textbox.Text += (char)key.Unicode;
                  break;
               }
            }
         }
      }
   }

   public void on_mouseEntered(){
      isMouseOnTop = true;
   }

   public void on_mouseExited(){
      isMouseOnTop = false;
   }

   public void on_compiled(ScriptLoader.programdata pd){
      try{
         GD.PrintErr("test");
         bot2.setProgramData(pd);
         bot2.runProgram();
      }
      catch(Exception e){
         GD.PrintErr(e.ToString());
      }
   }

   public void on_buttonPressed(){
      sloader.PickAndCompileScript(on_compiled);
      //sloader.test1();
      
      /*
      bot1.setProgramPath(bot1.compileCode(textbox.Text.Remove(0, startTextLength)));
      bot1.runProgram();
      */
   }

   public void on_buttonPressed2(){
      //bot2.setProgramPath(bot2.compileCode(textbox.Text.Remove(0, startTextLength)));
      //bot2.runProgram();
   }
}