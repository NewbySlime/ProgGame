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
        Int of index in the dictionary. Returns -1 if not found.
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

        if(left <= right)
          return -1;

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

    public void setProgramData(ScriptLoader.programdata pd, string additionalArguments = ""){
      currentProgdat = pd;
      progrun.changePathprog(sloader.GetAboslutePathToProg(pd), defaultArgument + " " + additionalArguments);
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
          //GD.PrintErr("Waiting a socket...");
          Socket handle = (await currentListener.AcceptAsync());
          //GD.PrintErr("Accepting a socket...");
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
              // intentionally empty
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
                
                if(returnedrf.isReadyToUse){
                  GD.PrintErr("ready to send");
                  ProcessedData.Add(returnedrf);
                }
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

  // should just use the list of it
  // and returns the class based on the index
  public class Backpack{

    //id of [0,1,2,3,] are reserved for [weapon, ammo, consumables, armor]
    private class bp_itemdata{
      // items with extended data, will only have 1 max in the backpack

      private int _manyitems = 0;
      private int _maxitems;
      private itemdata.DataType _type;
      private int _itemid;

      public object extendedData;

      public int MaxItems{
        get{
          return _maxitems;
        }
      }

      public int ItemCount{
        get{
          return _manyitems;
        }
      }

      public int ItemID{
        get{
          return _itemid;
        }
      }

      public itemdata.DataType Type{
        get{
          return _type;
        }
      }


      public bp_itemdata(itemdata idat, int maxitem){
        _itemid = idat.itemid;
        _type = idat.type;
        _maxitems = maxitem;
      }

      // add or subtract the item count
      // returns excess number
      public int ChangeItemCount(int valueToAdd){
        int excess = 0;
        _manyitems += valueToAdd;
        if(_manyitems > _maxitems){
          excess = _manyitems - _maxitems;
          _manyitems = _maxitems;
        }
        else if(_manyitems < 0){
          excess = 0 - _manyitems;
          _manyitems = 0;
        }

        return excess;
      }
    }


    //storage index contains ints based on itemdatas
    //basically index to itemdatas
    //if the value is -1, then the storage isn't used
    private int[] indexbp;

    //sorted storage based on id
    //the second variable is for index in backpack
    private List<KeyValuePair<bp_itemdata, int>> itemdatas = new List<KeyValuePair<bp_itemdata, int>>();

    //index of unoccupied in indexbp
    //this is used when the backpack needs to know the first void storage
    //only available if a gap exists in between 0 and bp_i
    private CustomDict<int> unoccupiedIndex = new CustomDict<int>();

    //referencing itemdatas
    private bool isNewItemsSorted = false;

    private int backpacksize, bp_i = 0;


    private void _quicksort(int lowest, int highest){
      if(lowest >= 0 && highest >= 0 && lowest < highest){
        int part = _partition(lowest, highest);
        _quicksort(lowest, part-1);
        _quicksort(part+1, highest);
      }
    }

    // check this
    // idx is based on itemdatas' index
    private void swaplist(int idx1, int idx2){
      var pair1 = itemdatas[idx1];
      var pair2 = itemdatas[idx2];

      int idxbp1 = pair1.Value, idxbp2 = pair2.Value;

      // swap index based on backpack
      indexbp[idxbp1] = idx2;
      indexbp[idxbp2] = idx1;

      // swap variables in the list
      itemdatas[idx1] = pair2;
      itemdatas[idx2] = pair1;
    }

    // check the sort
    private int _partition(int lowest, int highest){
      int p_id = itemdatas[highest-1].Key.ItemID, p_type = (int)itemdatas[highest-1].Key.Type;
      int pivot_i = lowest;
      for(int i = lowest; i < highest-1; i++){
        bp_itemdata tmp = itemdatas[i].Key;
        if(tmp.ItemID < p_id || tmp.ItemID == p_id && (int)tmp.Type <= p_type){
          swaplist(i, pivot_i);
          pivot_i++;
        }
      }

      swaplist(highest-1, pivot_i++);
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
    //this might be in the middle, consider using getFirstIndex or getLastIndex
    private int getIndex(int id, itemdata.DataType type){
      if(!isNewItemsSorted)
        doSortIndex();

      int res = -1, left = 0, right = itemdatas.Count-1;
      while(left <= right){
        int i = Mathf.FloorToInt((left+right)/2);
        bp_itemdata refitem = itemdatas[i].Key;
        if(refitem.ItemID < id || (int)refitem.Type < (int)type)
          left = i+1;
        else if(refitem.ItemID > id || (int)refitem.Type > (int)type)
          right = i-1;
        else{
          res = itemdatas[i].Value;
          break;
        }
      }

      return res;
    }

    //actually the same as getIndex, but returns the first one
    private int getFirstIndex(int id, itemdata.DataType type){
      int idx = getIndex(id, type);
      if(idx < 0)
        return idx;
      else{
        int i = idx;
        bp_itemdata refitem = itemdatas[i--].Key;
        while(refitem.ItemID == id && refitem.Type == type && i >= 0)
          refitem = itemdatas[i--].Key;
        
        return i+1;
      }
    }

    private int getLastIndex(int id, itemdata.DataType type){
      int idx = getIndex(id, type);
      if(idx < 0)
        return idx;
      else{
        int i = idx;
        bp_itemdata refitem = itemdatas[i++].Key;
        while(refitem.ItemID == id && refitem.Type == type && i < itemdatas.Count)
          refitem = itemdatas[i++].Key;

        return i-1;
      }
    }

    // return index of last in backpack, if a same item present in backpack
    // or, return index of the backpack that is free
    // if use_it is true, the function will account the storage usage
    private int getFreeIndex(int itemID, itemdata.DataType type, bool use_it){
      int maxitem = WorldDefinition.autoload.GetItemMax(type);
      int idx = getLastIndex(itemID, type);
      GD.Print("Get index: ", idx);
      if((idx < 0 && getFreeStorageCount() > 0) || (idx >= 0 && itemdatas[idx].Key.ItemCount >= maxitem))
        if(unoccupiedIndex.Length > 0){
          int index = unoccupiedIndex[0];
          if(use_it)
            unoccupiedIndex.Remove(index);

          return index;
        }
        else{
          int index = bp_i;
          if(use_it)
            bp_i++;
          
          GD.Print("bp_i: ", index);
          return index;
        }

      GD.Print("Last: ", idx);
      return idx;
    }

    private int getFreeStorageCount(){
      return unoccupiedIndex.Length + (backpacksize - bp_i);
    }

    private static itemdata constructItemdata(in bp_itemdata data){
      itemdata res = new itemdata{
        itemid = data.ItemID,
        type = data.Type,
        quantity = data.ItemCount,
        extendedData = null
      };

      return res;
    }

    
    public Backpack(int size){
      backpacksize = size;
      indexbp = new int[backpacksize];
    }

    // returns number of items that doesn't get cut
    public int CutItems(int itemid, itemdata.DataType itemtype, int quantity){
      doSortIndex();

      int lastIndex = getLastIndex(itemid, itemtype);
      if(lastIndex < 0)
        return quantity;
      
      int idx = lastIndex;
      quantity = -quantity;
      while(quantity != 0 && idx >= 0){
        var idataPair = itemdatas[idx];
        quantity = idataPair.Key.ChangeItemCount(quantity);
        if(quantity != 0){
          itemdatas.RemoveAt(idx);
          indexbp[idataPair.Value] = -1;
          unoccupiedIndex.AddClass(idx, idx);
        }

        idx--;
      }
      
      return quantity;
    }

    // if the backpack is full, it returns the excess number
    // if the data is more than maxitem, index parameter won't be used
    // 
    // returns > 0 if there are excess
    public int AddItem(itemdata data, int index = -1){
      if(data.quantity <= 0)
        return 0;

      // maxitem should be fixed and based on the item type
      int maxitem = WorldDefinition.autoload.GetItemMax(data.type);

      // should get the same item type in the backpack, then add it from there
      int excess = data.quantity;
      index = index < backpacksize && index >= 0 && indexbp[index] == -1? index: getFreeIndex(data.itemid, data.type, true);

      isNewItemsSorted = false;

      // if index is -1, then break;
      while(excess != 0 && itemdatas.Count < backpacksize){
        // add if the index is already used, then add it from there
        bp_itemdata currbp = new bp_itemdata(data, maxitem);
        excess = currbp.ChangeItemCount(excess);
        itemdatas.Add(new KeyValuePair<bp_itemdata, int>(currbp, index));

        GD.Print("Currindex: ", index, " excess: ", excess);
        for(int i = 0; i < itemdatas.Count; i++)
          GD.Print("id: ", itemdatas[i].Key.ItemID, " idxbp: ", indexbp[i]);
        GD.Print("");
        
        indexbp[index] = itemdatas.Count -1;

        if(excess != 0)
          index = getFreeIndex(data.itemid, data.type, true);
      }

      return excess;
    }

    public int HowManyItems(int itemid, itemdata.DataType itemtype){
      int res = 0;

      int index = getIndex(itemid, itemtype);
      res += itemdatas[index].Key.ItemCount;

      for(int i = 1; i >= -1; i -= 2){
        int i_index = index + i;
        while(i_index >= 0 && i_index < itemdatas.Count){
          if(itemdatas[i_index].Key.ItemID == itemid)
            res += itemdatas[i_index].Key.ItemCount;
          else
            break;
          
          i_index += i;
        }
      }

      return res;
    }

    public void ChangeIndex(uint from, uint to){
      try{
        if(indexbp[from] == -1)
          return;
        
        int tmp = indexbp[from];
        if(indexbp[to] >= 0){
          indexbp[from] = indexbp[to];
          var _tmpbpdat = itemdatas[indexbp[to]].Key;
          itemdatas[indexbp[to]] = new KeyValuePair<bp_itemdata, int>(_tmpbpdat, (int)from);
        }
        
        indexbp[to] = tmp;
        var tmpbpdat = itemdatas[tmp].Key;
        itemdatas[tmp] = new KeyValuePair<bp_itemdata, int>(tmpbpdat, (int)to);
      }
      catch(System.Exception e){
        GD.PrintErr("Cannot swap item between idx: (", from, ") to idx: (", to, ").\nError msg: ", e.ToString(), "\n");
      }
    }

    public itemdata?[] GetItemData(){
      itemdata?[] idata = new itemdata?[backpacksize];
      for(int i = 0; i < backpacksize && i < bp_i; i++)
        if(indexbp[i] > -1)
          idata[i] = constructItemdata(itemdatas[indexbp[i]].Key);
        else
          idata[i] = null;
      
      return idata;
    }

    public itemdata? GetItemData(int idx){
      if(idx >= 0 && idx < backpacksize && indexbp[idx] != -1)
        return constructItemdata(itemdatas[indexbp[idx]].Key);
      
      return null;
    }

    public bool IsStorageAvailable(int idx){
      if(idx >= 0 && idx < backpacksize)
        return indexbp[idx] != -1;
      else
        GD.PrintErr("Cannot get item in idx: (", idx, ")\n");

      return false;
    }
  }
}
