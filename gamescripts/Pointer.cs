using Godot;

public class Pointer: Position2D{
  public struct CrosshairOption{
    public bool up_line, right_line, down_line, left_line, dot;
    public float crosshairSize;
  }

  public struct PointerOption{
    public bool isVisible;
  }

  public enum PointerContext{
    RegularPointer,
    Crosshair
  }

  private Sprite[] LineSprites;
  private Sprite DotSprite;
  private Sprite PointerSprite;
  private CrosshairOption currentCOption = new CrosshairOption();
  private PointerOption currentPOption = new PointerOption();
  private PointerContext currentPContext = PointerContext.RegularPointer;

  public CrosshairOption CurrentCrosshairOption{
    set{
      currentCOption = value;
      if(currentPContext == PointerContext.Crosshair)
        UseCrosshairOption(currentCOption);
    }
  }

  public PointerOption CurrentPointerOption{
    set{
      currentPOption = value;
      if(currentPContext == PointerContext.RegularPointer)
        UsePointerOption(currentPOption);
    }
  }

  private void UseCrosshairOption(CrosshairOption opt){
    bool[] bool_array = new bool[]{opt.up_line, opt.right_line, opt.down_line, opt.right_line};
    for(int i = 0; i < bool_array.Length; i++){
      LineSprites[i].Visible = bool_array[i];
      LineSprites[i].Scale = Vector2.One * opt.crosshairSize;
    }

    DotSprite.Visible = opt.dot;
    DotSprite.Scale = Vector2.One * opt.crosshairSize;
  }

  private void UsePointerOption(PointerOption opt){
    PointerSprite.Visible = opt.isVisible;
  }

  public StreamTexture LineSpriteImage{
    set{
      foreach(Sprite sprite in LineSprites)
        sprite.Texture = value;

    }
  }

  public StreamTexture DotSpriteImage{
    set{
      DotSprite.Texture = value;
    }
  }

  public StreamTexture PointerSpriteImage{
    set{
      PointerSprite.Texture = value;
    }
  }


  public override void _Ready(){
    ZIndex = 1;

    LineSprites = new Sprite[4];
    for(int i = 0; i < 4; i++){
      Sprite l_s = new Sprite();
      l_s.RotationDegrees = 90 * i;
      AddChild(l_s);
      LineSprites[i] = l_s;
    }

    DotSprite = new Sprite();
    AddChild(DotSprite);
    DotSprite.ZIndex = 1;

    PointerSprite = new Sprite();
    AddChild(PointerSprite);
  }

  public void SetDiameter(float pixels){
    float ninetyDegRad = Mathf.Deg2Rad(90);
    for(int i = 0; i < 4; i++)
      LineSprites[i].Position = new Vector2(Mathf.Cos(ninetyDegRad * (i-1)), Mathf.Sin(ninetyDegRad * (3-i))) * pixels;

  }

  public void SetPointerContext(PointerContext pc){
    currentPContext = pc;
    switch(pc){
      case PointerContext.RegularPointer:{
        UseCrosshairOption(new CrosshairOption{
          up_line = false,
          right_line = false,
          down_line = false,
          left_line = false,
          dot = false
        });

        UsePointerOption(currentPOption);
        break;
      }

      case PointerContext.Crosshair:{
        UsePointerOption(new PointerOption{
          isVisible = false
        });

        UseCrosshairOption(currentCOption);
        break;
      }
    }
  }
}