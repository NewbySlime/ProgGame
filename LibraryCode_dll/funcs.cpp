#include "coredll.h"
#include "funcs_list.h"


struct funcinfo{
  unsigned short functionCode;
  string strparam;

  funcinfo(unsigned short fc = 0, string str = ""){
    functionCode = fc;
    strparam = str;
  }
};

class FunctionHandler{
  private:
    IOSocket *io;
    map<unsigned short, string> StringParamsOpr {
      {FUNCS_NORETURN, string(FUNCSPARAMS_RETURNVOID)},
      {FUNCS_PROGRAM_EXIT, string(FUNCSPARAMS_TERMINATEPROG)}
    };

    map<unsigned short, string> StringParamsSnd {
      {FUNCS_MOVEPOS, string(FUNCSPARAMS_MOVEPOS)},
      {FUNCS_MOVEFRONTBACK, string(FUNCSPARAMS_MOVEFRONTBACK)},
      {FUNCS_TURNDEG, string(FUNCSPARAMS_TURNDEG)}
    };

    map<unsigned short, string> StringParamsReq {
      {FUNCS_REQPOS, string(FUNCSPARAMS_REQPOS)},
      {FUNCS_REQANGLEDEG, string(FUNCSPARAMS_REQANDLEDEG)}
    };

    map<unsigned short, objData> returnedObj;
    map<unsigned short, condition_variable*> WaitingList;
    mutex WaitingList_m, returnedObj_m;


    void _AtDataGet(objData obj){
      //cerr << "_AtDataGet called" << endl;
      {lock_guard<mutex> lg1(WaitingList_m), lg2(returnedObj_m);
        returnedObj[obj.functionID] = obj;
        auto iter = WaitingList.find(obj.functionID);
        if(iter != WaitingList.end())
          iter->second->notify_all();
      }
      //cerr << "_AtDataGet Done" << endl;
    }

    map<unsigned short, string> *_GetStringParamTemplate(int templateCode){
      switch(templateCode){
        break; case oprMagicNum:
          return &StringParamsOpr;
        
        break; case sendMagicNum:
          return &StringParamsSnd;
        
        break; case reqMagicNum:
          return &StringParamsReq;
        
        break; default:
          return nullptr;
      }
    }

    string _GetStringParam(int templateCode, unsigned short functionCode){
      auto strparamptr = _GetStringParamTemplate(templateCode);
      if(strparamptr != nullptr && strparamptr->find(functionCode) != strparamptr->end())
        return strparamptr->operator[](functionCode);

      return "";
    }

    void static AtDataGet(objData obj, void* thisClass){
      //cerr << "AtDataGet called" << endl;
      ((FunctionHandler*)thisClass)->_AtDataGet(obj);
    }

    string static GetStringParam(int tc, unsigned short fc, void* thisClass){
      return ((FunctionHandler*)thisClass)->_GetStringParam(tc, fc);
    }

    //note: not thread safe
    unsigned short GetFreeRandomID(){
      unsigned short rand = 0;
      do{
        rand = (unsigned short)RandomizerClass::getrandom();
      }while(WaitingList.find(rand) != WaitingList.end());

      return rand;
    }


  public:
    FunctionHandler(){
      io = new IOSocket();
      io->ptrToClassCallback = this;
      io->callbackFunction = &AtDataGet;
      io->cbToGetParamLen = &GetStringParam;
      io->startSocket();
    }

    ~FunctionHandler(){
      io->stopSocket();
      delete io;
    }

    void AddStringParam(int templateCode, unsigned short functionCode, string strParam){
      auto strparamptr = _GetStringParamTemplate(templateCode);
      strparamptr->operator[](functionCode) = strParam;
    }

    bool IsStringParamAvailable(int templateCode, unsigned short functionCode){
      auto strparamptr = _GetStringParamTemplate(templateCode);
      return (strparamptr->find(functionCode) != strparamptr->end());
    }

    objData callFunction(objData currentObjData){
      //cerr << "callFunction called" << endl;
      //this_thread::sleep_for(chrono::seconds(1));
      mutex m;
      unique_lock<mutex> ul(m);
      condition_variable cv;
      {lock_guard<mutex> lg(WaitingList_m);
        currentObjData.functionID = GetFreeRandomID();
        WaitingList[currentObjData.functionID] = &cv;
      }

      //cerr << "waiting for the function" << endl;
      io->queueData(currentObjData);
      cv.wait(ul);

      {lock_guard<mutex> lg(WaitingList_m);
        WaitingList.erase(currentObjData.functionID);
      }

      objData newObj;
      {lock_guard<mutex> lg(returnedObj_m);
        newObj = returnedObj[currentObjData.functionID];
        returnedObj.erase(currentObjData.functionID);
      }

      return newObj;
    }
};

FunctionHandler *currentfh;

void initializeFunc(){
  currentfh = new FunctionHandler();
}

void closeFunc(){
  delete currentfh;
}

void moveTo(float x, float y){
  objData obj;
  obj.templateCode = sendMagicNum;
  obj.functionCode = FUNCS_MOVEPOS;
  obj.params = getParam(x) + getParam(y);
  currentfh->callFunction(obj);
}

void turnBy(float degrees){
  objData obj;
  obj.templateCode = sendMagicNum;
  obj.functionCode = FUNCS_TURNDEG;
  obj.params = getParam(degrees);
  currentfh->callFunction(obj);
}

vec2<float> currentPosition(){
  objData obj;
  obj.templateCode = reqMagicNum;
  obj.functionCode = FUNCS_REQPOS;
  //cerr << "awaiting for a data..." << endl;
  objData newobj = currentfh->callFunction(obj);
  //cerr << "data is done used" << endl;
  vec2<float> currentPos(getParam<float>(newobj.params, 0), getParam<float>(newobj.params, 4));
  //cerr << "done calculating" << endl;
  return currentPos;
}

float currentAngleDeg(){
  objData obj;
  obj.templateCode = reqMagicNum;
  obj.functionCode = FUNCS_REQANGLEDEG;
  objData newobj = currentfh->callFunction(obj);
  float currentangled = getParam<float>(newobj.params, 0);
  return currentangled;
}