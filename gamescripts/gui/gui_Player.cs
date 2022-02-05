using Godot;

public class gui_Player: Control{
  TextureProgress healthProgress, energyProgress;

  public override void _Ready(){
    healthProgress = GetNode<TextureProgress>("HealthProgress");
    energyProgress = GetNode<TextureProgress>("EnergyProgress");
  }

  public void Change_Health(float val){
    healthProgress.Value = healthProgress.MaxValue * val;
  }

  public void Change_Energy(float val){
    energyProgress.Value = energyProgress.MaxValue * val;
  }
}