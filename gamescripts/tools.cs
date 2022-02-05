using System;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Tools.Storage;
using Godot;

namespace Tools{
  namespace Storage{

    /**
      <summary>
      The use of this instead of regular Dict by c# is so the element can be accessed directly by index and key at the same time
      </summary>
    */
    public class CustomDict<TClass>{
      private List<TClass> classlist = new List<TClass>();
      private List<int> keylist = new List<int>();
      private bool isSorted = false;

      /**
        <summary>
        This is for sorting all the elements by using quicksort
        </summary>
      */
      private void quicksort(int lowest, int highest){
        if(lowest >= 0 && highest >= 0 && lowest < highest){
          int part = partition(lowest, highest);
          quicksort(lowest, part);
          quicksort(part+1, highest);
        } 
      }

      /**
        <summary>
        Part of the quicksort fumction
        </summary>
      */
      private int partition(int lowest, int highest){
        int pivot = keylist[highest-1];
        int pivot_i = lowest;
        for(int i = lowest; i < highest-1; i++){
          if(keylist[i] <= pivot){
            int keytemp = keylist[i];
            keylist[i] = keylist[pivot_i];
            keylist[pivot_i] = keytemp;

            TClass classtemp = classlist[i];
            classlist[i] = classlist[pivot_i];
            classlist[pivot_i] = classtemp;
            pivot_i++;
          }
        }

        int keytmp = keylist[highest-1];
        keylist[highest-1] = keylist[pivot_i];
        keylist[pivot_i] = keytmp;

        TClass classtmp = classlist[highest-1];
        classlist[highest-1] = classlist[pivot_i];
        classlist[pivot_i] = classtmp;
        return pivot_i;
      }

      /**
        <summary>
        Getting the element based on the index of the element (not key)
        </summary>
      */
      public TClass this[int i]{
        get{
          return classlist[i];
        }
      }

      public int Length{
        get{
          return classlist.Count;
        }
      }

      /**
        <summary>
        Adding element to the dictionary based on the key
        </summary>
      */
      public void AddClass(int key, TClass tclass, bool dosort = true){
        keylist.Add(key);
        classlist.Add(tclass);
        isSorted = false;

        if(dosort)
          SortList();
      }

      /**
        <summary>
        Function to deal with the sorting in the dictionary
        </summary>
      */
      public void SortList(){
        quicksort(0, classlist.Count);
        isSorted = true;
      }

      /**
        <summary>
        Function to get the object based on the key
        </summary>

        <returns>
        object inside the dictionary
        </returns>
      */
      public object find(int key){
        int index = findkey(key);
        return classlist[index];
      }

      /**
        <summary>
        Function to get the index of the object based on key
        </summary>

        <returns>
        int of index in the dictionary
        </returns>
      */
      public int findkey(int key){
        if(!isSorted)
          SortList();

        int res = -1, left = 0, right = keylist.Count-1;
        while(left <= right){
          int i = Mathf.FloorToInt((left+right)/2);
          if(keylist[i] < key)
            left = i+1;
          else if(keylist[i] > key)
            right = i-1;
          else{
            res = i;
            break;
          }
        }

        return res;
      }

      /**
        <summary>
          Function to remove an object based on the key
        </summary>
      */
      public void Remove(int key){
        int index = findkey(key);
        keylist.RemoveAt(index);
        classlist.RemoveAt(index);
      }
    }
  }

  /**
    <summary>
      An enumeration for holding a 4 byte of code to recognize a request or sending data in between game and the child programs (programs for the bot functionality)
    </summary>
  */
  public enum templateCode_enum{
    sendCode = 0x0b534e44,
    reqCode = 0x0b524551,
    oprCode = 0x0b6f7072,
    lineTerminatorCode = (int)'\n'
  }

  /**
    <summary>
      A data struct for holding sent or requested data
    </summary>
  */
  public class returnFunc{
    //it has to be primitives
    public Int32 TemplateCode;
    //funccode is a the code of a function
    //funcid is the program id
    public ushort FuncCode, FuncID;
    public bool isReadyToUse;
    //public string ParamStr;
    public byte[] ParamBytes = new byte[0];
    
    public void AppendParam(byte[] bytearr, int length, int start = 0){
      if(length+start >= bytearr.Length)
        length = bytearr.Length - start;

      int previoussize = ParamBytes.Length;
      Array.Resize<byte>(ref ParamBytes, ParamBytes.Length + length);
      for(int i = 0; i < length; i++)
        ParamBytes[previoussize+i] = bytearr[start+i];
    }
  }


  public class ProgramRunner{
    protected Dictionary<System.Int16, String> functionDict = new Dictionary<short, string>();
    private Process process = null;

    private String queueStr = "", pathprogram = "", arguments = "";
    private bool dowriteBinary_b = true, isWriteBinStopped = false, isProgramRunning = false;

    public delegate void StdioEvent(object obj, String s); 
    public delegate void AtFuncCalled(returnFunc obj);
    public AtFuncCalled atFuncCalled_f;
    public StdioEvent atErrorEvent;
    public Task currentReadTask, currentWriteTask;
    public bool isRunning{
      get{
        return isProgramRunning;
      }
    }

    private void dumpFunc(object obj, String s){
      GD.PrintErr("Cerr from child: ", s);
    }
    private void dumpFunc1(returnFunc obj){}


    private async Task writeBinary(){
      while(dowriteBinary_b){
        String qStr = "";
        lock(queueStr){
          if(queueStr.Length <= 0){
            isWriteBinStopped = true;
          }
          else{
            qStr = queueStr;
            queueStr = "";
          }
        }

        if(isWriteBinStopped)
          break;

        await process.StandardInput.WriteAsync(qStr);
      }
    }


    private void atProcessExit(object obj, EventArgs args){
      GD.Print("Program exited");
    }

    public ProgramRunner(string filePath = "", string argument = ""){
      if(filePath != ""){
        changePathprog(filePath, argument);
        createProcess(filePath, argument);
      }
    }

    /*
      bytes has the contents of chars indicating how many bytes every parameters is, the max of bytes can be used is 255 for a string
    */
    public void addFunctionCode(short funcCode, string funcBytes){
      functionDict.Add(funcCode, funcBytes);
    }

    public int doRun(){
      if(pathprogram != ""){
        try{
          createProcess(pathprogram, arguments);
          process.Start();
          process.BeginErrorReadLine();
          currentWriteTask = writeBinary();
          isProgramRunning = true;
        }
        catch(Exception e){
          GD.PrintErr(e.Message, "\n", e.StackTrace);
        }
      }
      else
        GD.PrintErr("Filename isn't specified.");

      return 0;
    }

    public void createProcess(string pathprog, string arguments = ""){
      process = new Process{
        StartInfo = new ProcessStartInfo{
          FileName = pathprog,
          Arguments = arguments,
          //RedirectStandardOutput = true,
          RedirectStandardError = true,
          RedirectStandardInput = true,
          CreateNoWindow = true,
          UseShellExecute = false,
        },
        EnableRaisingEvents = true
      };

      atErrorEvent = dumpFunc;
      atFuncCalled_f = dumpFunc1;

      process.ErrorDataReceived += new DataReceivedEventHandler((sender, obj) =>{
        if(!String.IsNullOrEmpty(obj.Data)){
          atErrorEvent(sender, obj.Data); 
        }
      });

      process.Exited += new EventHandler((sender, obj) =>{
        atProcessExit(sender, obj);
        isProgramRunning = false;
      });
    }

    /*
      This won't have any effect on the current process
    */
    public void changePathprog(string pathprog, string arguments = ""){
      pathprogram = pathprog;
      
      if(arguments != "")
        this.arguments = arguments;
    }

    public void writeInputOnce(char c = '\0'){
      if(process != null && isProgramRunning)
        process.StandardInput.Write(c);
    }

    public void queueStringToStdin(String str){
      lock(queueStr){
        queueStr += str;
        if(isWriteBinStopped){
          isWriteBinStopped = false;
          currentWriteTask = writeBinary();
        } 
      }
    }

    public void doTerminateProcess(){
      if(isProgramRunning){
        lock(process){
          process.Kill();
          dowriteBinary_b = false;
        }

        isProgramRunning = false;
      }
    }
  }

  public class ProgrammableObject : Node2D{
    private string currentProgramPath, defaultArgument;
    private ScriptLoader sloader;
    private ScriptLoader.programdata currentProgdat;

    protected ProgramRunner progrun;
    protected FunctionHandler functionHandler;

    public ushort currentpid = 0;

    public override void _Ready(){
      functionHandler = new FunctionHandler(GetNode<Autoload>("/root/Autoload").getcurrentSocketListener);

      ushort currentport = GetNode<Autoload>("/root/Autoload").GetcurrentPort();
      currentpid = functionHandler.getpid;
      sloader = GetNode<ScriptLoader>("/root/ScriptLoader");

      defaultArgument = "-port=" + currentport + " -pid=" + currentpid;
      GD.PrintErr("argument: ", defaultArgument);
        
      progrun = new ProgramRunner("", defaultArgument);
    }

    public override void _Notification(int what){
      switch(what){
        case NotificationPredelete:{
          stopProgram();
          break;
        }
      }
    }

    public void addFunctions(FunctionHandler.funcinfo[] functions){
      for(int i = 0; i < functions.Length; i++)
        functionHandler.AddCallbackFunc(functions[i]);
    }

    public void setProgramPath(string progPath, string additionalArguments = ""){
      progrun.changePathprog(progPath, defaultArgument + additionalArguments);
    }

    public string compileCode(string srccode){
      currentProgdat = new ScriptLoader.programdata{
        srcname = srccode
      };

      return sloader.CompileProgram(ScriptLoader.compilerpath.cpp_compiler, ref currentProgdat);
    }

    public void runProgram(){
      sloader.AddUsageOfProgram(currentProgdat);

      if(!progrun.isRunning)
        progrun.doRun();

      //give error handler
    }

    public void stopProgram(){
      functionHandler.QueueAsynclyReturnedObj(new returnFunc{
        TemplateCode = (int)templateCode_enum.oprCode,
        FuncCode = (ushort)ParamData.RegularBotFuncCodes.program_exit_code,
        isReadyToUse = true
      });

      progrun.writeInputOnce();
      //progrun.doTerminateProcess();
    }
  }


  public class SocketListenerHandler{
    private const ushort MaxSocketToListen = 50;
    private ushort _currentport;
    private Socket currentListener;
    private bool keepListening = true;
    private Dictionary<ushort, CallbackFunction> ProcIDCallback = new Dictionary<ushort, CallbackFunction>();
      //based on process id
    private Dictionary<ushort, List<returnFunc>> AsyncReturnedObj = new Dictionary<ushort, List<returnFunc>>();
    private CallbackFunction2 GetStringParam;


    public ushort currentport{
      get{
        return _currentport;
      }
    }

    public delegate void CallbackFunction(returnFunc rf, ref returnFunc rfRef);
    public delegate String CallbackFunction2(int templateCode, ushort code);

    
    public SocketListenerHandler(CallbackFunction2 cbToStringdb){
      _currentport = GetRandomFreePort();
      GetStringParam = cbToStringdb;
    }

    public SocketListenerHandler(CallbackFunction2 cbToStringdb, ushort port){
      _currentport = port;
      GetStringParam = cbToStringdb;
    }

    public ushort GetRandomFreePort(){
      ushort randomPort = (ushort)GD.Randi();
      TcpConnectionInformation[] tci = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpConnections();
      for(int i = 0; i < tci.Length; i++){
        if(tci[i].LocalEndPoint.Port == randomPort){
          randomPort = (ushort)GD.Randi();
          i = 0;
        }
      }

      return randomPort;
    }

    public void QueueReturnObj(ushort ProcessID, returnFunc rf){
      lock(AsyncReturnedObj){
        if(!AsyncReturnedObj.ContainsKey(ProcessID)){
          AsyncReturnedObj[ProcessID] = new List<returnFunc>();
        }

        AsyncReturnedObj[ProcessID].Add(rf);
        GD.PrintErr("new cobjs len: ", AsyncReturnedObj[ProcessID].Count, " PID: ", ProcessID);
      }
    }

    //returns process ID if current id isn't available
    public ushort AddProcID(CallbackFunction callback, ushort PID = 0){
      lock(ProcIDCallback){
        while(ProcIDCallback.ContainsKey(PID))
          PID = (ushort)GD.Randi();
        
        ProcIDCallback[PID] = callback;
      }

      return PID;
    }

    public Task StartListening(){
      return StartListening(currentport);
    }

    public async Task StartListening(ushort port){
      GD.PrintErr("Start listening...");
      keepListening = true;
      int bufLen = 255;
      byte[] recvbuf = new byte[bufLen];
      IPAddress localAddr = new IPAddress(new byte[]{127,0,0,1});
      IPEndPoint endPoint = new IPEndPoint(localAddr, port);
      currentListener = new Socket(localAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

      try{
        currentListener.Bind(endPoint);
        currentListener.Listen(MaxSocketToListen);
        while(keepListening){
          Socket handle = (await currentListener.AcceptAsync());
          GD.PrintErr("Accepting a socket...");
          byte[] DataFromSocket = new byte[0];
          int bytesLenRecv = 1;
          while(true){
            bytesLenRecv = handle.Receive(recvbuf);
            if(bytesLenRecv > 1){
              int sizebefore = DataFromSocket.Length;
              Array.Resize<byte>(ref DataFromSocket, DataFromSocket.Length + bytesLenRecv);
              for(int i = 0; i < bytesLenRecv; i++)
                DataFromSocket[sizebefore + i] = recvbuf[i];
            }
            else
              break;
          }

          List<returnFunc> ProcessedData = new List<returnFunc>();
          ushort ProcessID = 0;
          if(DataFromSocket.Length >= 2)
            ProcessID = BitConverter.ToUInt16(DataFromSocket, 0);
            
          for(int data_iter = 2; data_iter < (DataFromSocket.Length-9); data_iter++){
            returnFunc
              currentrf = new returnFunc{

              },

              returnedrf = new returnFunc{
                isReadyToUse = false
              };

            bool doLoop = true;
            while(data_iter < (DataFromSocket.Length-4) && doLoop){
              currentrf.TemplateCode = BitConverter.ToInt32(DataFromSocket, data_iter);
            GD.Print("Current data: ", (int)DataFromSocket[data_iter]);
              switch(currentrf.TemplateCode){
                case (int)templateCode_enum.reqCode:
                case (int)templateCode_enum.oprCode:
                case (int)templateCode_enum.sendCode:{
                  doLoop = false;
                  data_iter += 3;
                  break;
                }
              }

              data_iter++;
            }

            currentrf.FuncCode = BitConverter.ToUInt16(DataFromSocket, data_iter);
            data_iter += 2;
            
            currentrf.FuncID = BitConverter.ToUInt16(DataFromSocket, data_iter);
            data_iter += 2;
            GD.PrintErr("FuncID: ", currentrf.FuncID);

            String param = GetStringParam(currentrf.TemplateCode, currentrf.FuncCode);
            for(int param_iter = 0; param_iter < param.Length; param_iter++){
              currentrf.AppendParam(DataFromSocket, (int)param[param_iter], data_iter);
              data_iter += (int)param[param_iter];
            }
            
            lock(ProcIDCallback){
              if(!ProcIDCallback.ContainsKey(ProcessID))
                GD.PrintErr("A Process ID cannot be find, ID: ", ProcessID);
              
              else{
                ProcIDCallback[ProcessID](currentrf, ref returnedrf);
                
                if(returnedrf.isReadyToUse)
                  ProcessedData.Add(returnedrf);
              }
            }

            while(data_iter+1 < DataFromSocket.Length && DataFromSocket[data_iter+1] != '\n')
              data_iter++;
            
            if(data_iter+1 < DataFromSocket.Length && DataFromSocket[data_iter+1] == '\0')
              data_iter = DataFromSocket.Length;
          }

          //for loop here for appending returned function from before
          lock(AsyncReturnedObj){
            if(AsyncReturnedObj.ContainsKey(ProcessID)){
              List<returnFunc> CurrentObjs = AsyncReturnedObj[ProcessID];
              GD.PrintErr("cobj len: ", CurrentObjs.Count);
              for(int ro_iter = 0; ro_iter < CurrentObjs.Count; ro_iter++){
                GD.PrintErr("  funcid: ", CurrentObjs[ro_iter].FuncID);
                ProcessedData.Add(CurrentObjs[ro_iter]);
              }

              AsyncReturnedObj.Remove(ProcessID);
            }
          }

          //then do some functions based on procID, and funcCode
          //if the program returned, then send it
          //if the program is run asynchronously, then just go on, since the child program will know if the parent will give acknowledge about the function via stdin

          for(int pd_iter = 0; pd_iter < ProcessedData.Count; pd_iter++){
            GD.Print(ProcessedData[pd_iter].FuncID);
            if(ProcessedData[pd_iter].isReadyToUse){
              handle.Send(BitConverter.GetBytes(ProcessedData[pd_iter].TemplateCode));
              handle.Send(BitConverter.GetBytes(ProcessedData[pd_iter].FuncCode));
              handle.Send(BitConverter.GetBytes(ProcessedData[pd_iter].FuncID));
              handle.Send(ProcessedData[pd_iter].ParamBytes);              
              handle.Send(new byte[]{(byte)'\n'});
            }
          }

          handle.Shutdown(SocketShutdown.Both);
          handle.Close();
        }
      }
      catch(Exception err){
        GD.Print(err);
      }
    }

    public void StopListening(){
      try{
        keepListening = false;
        currentListener.Close();
      }
      catch(Exception e){
        GD.PrintErr(e.ToString());
      }
    }
  }


  public class FunctionHandler{
    private ReferenceCallback refToMainSocket;
    private ushort pid;
    private Dictionary<ushort, callbackfunction> callbacksToCallOpr = new Dictionary<ushort, callbackfunction>();
    private Dictionary<ushort, callbackfunction> callbacksToCallSnd = new Dictionary<ushort, callbackfunction>();
    private Dictionary<ushort, callbackfunction> callbacksToCallReq = new Dictionary<ushort, callbackfunction>();

    public ushort getpid{
      get{
        return pid;
      }
    }

    public struct funcinfo{
      public int templateCode;
      public ushort funccode;
      public callbackfunction callback;
    }

    public delegate ref SocketListenerHandler ReferenceCallback();
    public delegate void callbackfunction(returnFunc rf, ref returnFunc refrf);

    public FunctionHandler(ReferenceCallback refc){
      refToMainSocket = refc;
      pid = refToMainSocket().AddProcID(AtFuncCalled);
    }

    public void AtFuncCalled(returnFunc currrf, ref returnFunc returnedrf){
      callbackfunction cf = null;
      bool getcallback = false;

      switch((templateCode_enum)currrf.TemplateCode){
        case templateCode_enum.oprCode:
          getcallback = callbacksToCallOpr.TryGetValue(currrf.FuncCode, out cf);
          break;
        
        case templateCode_enum.sendCode:
          getcallback = callbacksToCallSnd.TryGetValue(currrf.FuncCode, out cf);
          break;
        
        case templateCode_enum.reqCode:
          getcallback = callbacksToCallReq.TryGetValue(currrf.FuncCode, out cf);
          break;
      }
      
      if(getcallback && cf != null)
        cf(currrf, ref returnedrf);
    }

    public void AddCallbackFunc(funcinfo fi){
      switch((templateCode_enum)fi.templateCode){
        case templateCode_enum.oprCode:
          lock(callbacksToCallOpr)
            callbacksToCallOpr[fi.funccode] = fi.callback;
          
          break;
        
        case templateCode_enum.sendCode:
          lock(callbacksToCallSnd)
            callbacksToCallSnd[fi.funccode] = fi.callback;
          
          break;
        
        case templateCode_enum.reqCode:
          lock(callbacksToCallReq)
            callbacksToCallReq[fi.funccode] = fi.callback;

          break;  
      }
    }

    public void QueueAsynclyReturnedObj(returnFunc rf){
      refToMainSocket().QueueReturnObj(pid, rf);
    }
  }
}


namespace gametools{

  //id of [0,1,2,3,4] are reserved for [weapon, ammo, crafting_items, consumables, armor]
  public class itemdata{
    //currentweapondata is used to save the current state of a gun in backpack
    //while weapondata is used to save all information about the weapon
    public struct currentweapondata{
      public int weaponid;
      public int currentammo;
    }

    public struct consumablesdata{
      
    }

    public struct currentarmordata{

    }


    public enum datatype{
      weapon,
      ammo,
      crafting_items,
      consumables,
      armor
    }


    private int _manyitems = 0;
    public datatype type;
    public int itemid;
    public int maxitemsinbp;
    public int indexinbackpack;
    public object additionaldata;

    public int reducemanyitems(int many){
      int excess = (int)_manyitems+many;
      if(excess >= maxitemsinbp)
        return excess - maxitemsinbp;
      else if(excess < 0)
        return excess;
      else
        return 0;
    }

    public int getmanyitems(){
      return _manyitems;
    }
  }


  public class Backpack{
    //storage index contains ints based on itemdatas
    private int[] indexbp;
    //sorted storage based on id
    private List<itemdata> itemdatas = new List<itemdata>();
    //index of unoccupied in indexbp
    private CustomDict<int> unoccupiedIndex = new CustomDict<int>();
    //referencing itemdatas
    private bool isNewItemsSorted = false;
    private int backpacksize, bp_i = 0;

    private void _quicksort(int lowest, int highest){
      if(lowest >= 0 && highest >= 0 && lowest < highest){
        int part = _partition(lowest, highest);
        _quicksort(lowest, part);
        _quicksort(part+1, highest);
      }
    }

    private void swaplist(int idx1, int idx2){
      itemdata tmp = itemdatas[idx1];
      int bpidx = tmp.indexinbackpack, bpidx2 = itemdatas[idx2].indexinbackpack;
      int listidx = indexbp[bpidx], listidx2 = indexbp[bpidx2];
          
      tmp.indexinbackpack = bpidx2;
      indexbp[bpidx] = listidx2;

      itemdatas[idx2].indexinbackpack = bpidx;
      indexbp[bpidx2] = listidx;

      itemdatas[idx1] = itemdatas[idx2];
      itemdatas[idx2] = tmp;
    }

    private int _partition(int lowest, int highest){
      int p_id = itemdatas[highest-1].itemid, p_type = itemdatas[highest-1].itemid;
      int pivot_i = 0;
      for(int i = lowest; i < highest-1; i++){
        itemdata tmp = itemdatas[i];
        if(tmp.itemid <= p_id && (int)tmp.type <= (int)p_type){
          swaplist(i, pivot_i);
          pivot_i++;
        }
      }

      swaplist(highest-1, pivot_i);
      return pivot_i;
    }

    //uses quick sort
    private void doSortIndex(){
      if(!isNewItemsSorted){
        _quicksort(0, itemdatas.Count);
        isNewItemsSorted = true;
      }
    }

    //uses binary search
    private int getIndex(int id, itemdata.datatype type){
      if(!isNewItemsSorted)
        doSortIndex();

      int res = -1, left = 0, right = itemdatas.Count-1;
      while(left <= right){
        int i = Mathf.FloorToInt((left+right)/2);
        itemdata refitem = itemdatas[i];
        if(refitem.itemid < id && (int)refitem.type < (int)type)
          left = i+1;
        else if(refitem.itemid > id && (int)refitem.type > (int)type)
          right = i-1;
        else{
          res = itemdatas[i].indexinbackpack;
          break;
        }
      }

      return res;
    }
    
    public Backpack(int size){
      backpacksize = size;
      indexbp = new int[backpacksize];
    }

    public int AddItem(itemdata item, int indexinbp = -1){
      int returncode = 0;
      if(indexinbp < 0){
        if(unoccupiedIndex.Length > 0){
          indexbp[unoccupiedIndex[0]] = itemdatas.Count;
          item.indexinbackpack = unoccupiedIndex[0];
          itemdatas.Add(item);
          unoccupiedIndex.Remove((int)unoccupiedIndex[0]);
        }
        else if(bp_i < backpacksize){
          indexbp[bp_i] = itemdatas.Count;
          item.indexinbackpack = bp_i;
          itemdatas.Add(item);
          bp_i++;
        }
        else
          returncode = -1;

      }
      else if(indexbp[indexinbp] < 0){
        indexbp[indexinbp] = itemdatas.Count;
        item.indexinbackpack = indexinbp;
        itemdatas.Add(item);
        unoccupiedIndex.Remove(indexinbp);
      }
      else
        returncode = -1;

      if(returncode >= 0)
        isNewItemsSorted = false;
      
      return returncode;
    }

    public void SwapItem(int index1, int index2){
      if(index1 < backpacksize && index2 < backpacksize && index1 != index2 && index1 >= 0 && index2 >= 0){
        int itemdatas_i1 = indexbp[index1];
        int itemdatas_i2 = indexbp[index2];
        if(itemdatas_i1 >= 0){
          itemdatas[itemdatas_i1].indexinbackpack = itemdatas_i2;
          if(itemdatas_i2 >= 0)
            itemdatas[itemdatas_i2].indexinbackpack = itemdatas_i1;
          else{
            unoccupiedIndex.Remove(index1);
            unoccupiedIndex.AddClass(index2, index2);
          }

          indexbp[index1] = itemdatas_i2;
          indexbp[index2] = itemdatas_i1;
        }
      }
    }

    public itemdata RemoveItem(int index){
      int idx = indexbp[index];
      itemdata res = null;
      if(idx >= 0){
        itemdatas[idx].indexinbackpack = -1;
        res = itemdatas[idx];
        itemdatas.RemoveAt(idx);
        unoccupiedIndex.AddClass(idx, idx);
        indexbp[index] = -1;
      }

      return res;
    }

    public int HowManyItems(int id, itemdata.datatype type){
      int index = getIndex(id, type);
      int manyitems = 0;

      //for getting the lowest
      for(int lowest = index; lowest > 0; lowest--){
        itemdata tmp = itemdatas[lowest-1];
        if(tmp.itemid == id && tmp.type != type)
          manyitems += tmp.getmanyitems();
        else
          break;

      }

      //for getting the highest
      for(int highest = index; highest < itemdatas.Count; highest++){
        itemdata tmp = itemdatas[highest];
        if(tmp.itemid == id && tmp.type == type)
          manyitems += tmp.getmanyitems();
        else
          break;
      }

      return manyitems;
    }

    public int HowManyItemsInIdx(int index){
      return itemdatas[indexbp[index]].getmanyitems();
    }

    public itemdata GetItemdata(int index){
      return itemdatas[indexbp[index]];
    }

    //this will also remove some items
    public int CutItems(int id, itemdata.datatype type, int many){
      int maxidx = getIndex(id, type);
      int manyitemscutted = 0;
      for(; maxidx < backpacksize; maxidx++){
        itemdata tmp = itemdatas[indexbp[maxidx]];
        if(tmp.itemid != id || tmp.type != type){
          maxidx++;
          break;
        }
      }

      for(int index = maxidx-1; index >= 0; index--){
        itemdata tmp = itemdatas[indexbp[index]];
        if(tmp.itemid == id && tmp.type == type){
          int itemscount = tmp.getmanyitems();
          if(many > itemscount){
            many -= itemscount;
            manyitemscutted += itemscount;
            RemoveItem(index);
          }
          else{
            manyitemscutted += many;
            tmp.reducemanyitems(many);
            break;
          }
        }
        else
          break;
      }

      return manyitemscutted;
    }
  }
}
