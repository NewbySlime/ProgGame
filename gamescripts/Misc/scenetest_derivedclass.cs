using Godot;

public class scenetest_derivedclass: Node2D{
  private class1 cl = new class2();

  public override void _Ready(){
    base._Ready();

    AddChild(cl);
    GD.Print("Calling by function");
    cl.test();
    GD.Print("Calling by signal");
    cl.EmitSignal("TestSignal");
  }
}

public class class1: Node2D{
  [Signal]
  protected delegate void TestSignal();

  public override void _Ready(){
    Connect("TestSignal", this, "test");
  }

  public virtual void test(){
    GD.Print("Test");
  }
}

public class class2: class1{
  public override void _Ready(){
    base._Ready();
  }

  public override void test(){
    GD.Print("Test2");
  }
}