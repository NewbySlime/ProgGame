using Godot;
using System;
using System.Text;
using Tools;

using static Tools.FunctionHandler;


// this should use programrunner instead of scriptrunner
public class BotCore: ProgrammableObject{
  [Export]
  private uint robotSpeed = 100;
  [Export]
  private uint robotTurnSpeed = 30;

  [Signal]
  //at physics process
  private delegate void app();
  private RigidBody2D robotBody;
  private RID robotBody_rid;
 
  public override void _Ready(){
    base._Ready();

    robotBody = GetNode<RigidBody2D>("RobotBody");
    robotBody_rid = robotBody.GetRid();

    funcinfo[] thisfunc = new funcinfo[]{
      // --- operational funcinfos --- \\

      // --- send funcinfo --- \\
      new funcinfo{
        templateCode = (int)templateCode_enum.sendCode,
        callback = lib_moveto,
        funccode = (ushort)ParamData.RegularBotFuncCodes.moveTo_code
      },


      
      // --- request funcinfo --- \\
      new funcinfo{
        templateCode = (int)templateCode_enum.reqCode,
        callback = lib_reqpos,
        funccode = (ushort)ParamData.RegularBotFuncCodes.reqpos_code
      }
    };

    addFunctions(thisfunc);

    //setProgramPath("C:\\GodotGamePorjectMono\\unnamedHobbyProj\\LibraryCode_dll\\a.exe");
    //runProgram();
  }

  public override void _PhysicsProcess(float delta){
    EmitSignal("app");
  }


  public void atProgramError(object sender, String data){
    GD.PrintErr("Program Err, ", data);
  }

  //might need an ai
  public async void moveto(Vector2 to, ushort functionID){
    while(robotBody.Position != to){
      //GD.Print(robotBody.Position);
      await ToSignal(this, "app");
      
      float lengthToTarget = (to-robotBody.Position).Length();
      float physicsDeltaT = GetPhysicsProcessDeltaTime();
      float nextStep = Mathf.Clamp(robotSpeed * physicsDeltaT, 0, lengthToTarget);
      Physics2DDirectBodyState bodyState = Physics2DServer.BodyGetDirectState(robotBody_rid);
      bodyState.Transform = bodyState.Transform.Translated(nextStep*((to-robotBody.Position).Normalized()));
      robotBody._IntegrateForces(bodyState);
    }

    functionHandler.QueueAsynclyReturnedObj(new returnFunc{
      TemplateCode = (int)templateCode_enum.oprCode,
      FuncCode = 0,
      FuncID = functionID,
      isReadyToUse = true
    });

    lock(progrun)
      progrun.writeInputOnce();
  }

  // not tested
  // not added in cpp lib
  public async void movefrontback(float distance, ushort functionID){
    
  }
  
  public async void controlmotor(float power, ushort functionID){
    
  }
  
  public async void controlsteer(float angle_steer, ushort functionID){
    
  }

  public async void turn(float deg, ushort functionID){
    while(robotBody.GlobalRotationDegrees != deg){
      await ToSignal(this, "app");

      float lengthToTarget = deg-robotBody.RotationDegrees;
      float physicsDeltaT = GetPhysicsProcessDeltaTime();
      float nextStep = Mathf.Clamp(robotTurnSpeed * physicsDeltaT, 0, lengthToTarget);
      Physics2DDirectBodyState bodyState = Physics2DServer.BodyGetDirectState(robotBody_rid);
      bodyState.Transform = bodyState.Transform.Rotated(Mathf.Deg2Rad(deg));
      robotBody._IntegrateForces(bodyState);
    }

    functionHandler.QueueAsynclyReturnedObj(new returnFunc{
      TemplateCode = (int)templateCode_enum.oprCode,
      FuncCode = 0,
      FuncID = functionID,
      isReadyToUse = false
    });

    lock(progrun)
      progrun.writeInputOnce();
  }


  // --- send template functions --- \\
  public void lib_moveto(returnFunc returnFunc, ref returnFunc refrf){
    Vector2 toPos = new Vector2(
      BitConverter.ToSingle(returnFunc.ParamBytes, 0),
      BitConverter.ToSingle(returnFunc.ParamBytes, 4)
    );
    
    moveto(toPos, returnFunc.FuncID);
  }

  public void lib_movefrontback(returnFunc rf, ref returnFunc refrf){
    float distance = BitConverter.ToSingle(rf.ParamBytes, 0);
    movefrontback(distance, rf.FuncID);
  }

  public void lib_turndeg(returnFunc rf, ref returnFunc refrf){
    float degree = BitConverter.ToSingle(rf.ParamBytes, 0);
    turn(degree, rf.FuncID);
  }

  // --- request template functions --- \\
  public void lib_reqpos(returnFunc returnFunc, ref returnFunc refrf){
    refrf.isReadyToUse = true;
    refrf.TemplateCode = (int)templateCode_enum.reqCode;
    refrf.FuncCode = (ushort)ParamData.RegularBotFuncCodes.reqpos_code;
    refrf.FuncID = returnFunc.FuncID;
    Vector2 robotPos = robotBody.GlobalPosition;
    refrf.AppendParam(BitConverter.GetBytes(robotPos.x), 4);
    refrf.AppendParam(BitConverter.GetBytes(robotPos.y), 4);
  }

  public void lib_reqangledeg(returnFunc rf, ref returnFunc refrf){
    refrf.isReadyToUse = true;
    refrf.TemplateCode = (int)templateCode_enum.reqCode;
    refrf.FuncCode = (ushort)ParamData.RegularBotFuncCodes.reqrotdeg_code;
    refrf.FuncID = rf.FuncID;
    float currentangledeg = robotBody.GlobalRotationDegrees;
    refrf.AppendParam(BitConverter.GetBytes(currentangledeg), 2);
  }
}
