#include "coredll.h"

#ifdef _WIN32
  #define bufLength 255

  WSADATA *wsadat = NULL;
  uint16_t procID, port;

  void initSocket(unsigned short Port, unsigned short pid){
    wsadat = new WSADATA();   
    int res = WSAStartup(MAKEWORD(2,2), wsadat);
    if(res != 0){
      throw;
    }

    procID = pid;
    port = Port;
  }

  void closeSocket(){
    delete wsadat;
    WSACleanup();
  }

  // ** IOSocket functions **
  void IOSocket::socketThread(){
    try{
      while(keepSending){
        localHostAddr.sin_port = htons(port);
        size_t objQueuesize;
        objData *currobjs;
        {lock_guard<mutex> lg(objQueue_m);
          objQueuesize = objQueue.size();
          currobjs = new objData[objQueuesize];
          for(int i = 0; i < objQueuesize; i++){
            currobjs[i] = objQueue[i];
          }

          objQueue = vector<objData>();
        }

        if(objQueuesize <= 0 && !gettingData){
          mutex m;
          unique_lock<mutex> ul(m);
          isWaitingForData = true;
          //thread_cv.wait_for(ul, chrono::microseconds(100));
          thread_cv.wait(ul);
          continue;
        }

        gettingData = false;

        SOCKET s = socket(AF_INET, SOCK_STREAM, 0);
        if(connect(s, (sockaddr*)&localHostAddr, sizeof(localHostAddr)) < 0){
          delete[] currobjs;
          throw string("Socket fails to connect");
        }
        
        string ProcIDstr = getParam(procID);
        send(s, ProcIDstr.c_str(), ProcIDstr.length(), 0);

        for(size_t i = 0; i < objQueuesize; i++){
          objData *od = &currobjs[i];
          string paramtoSend;

          paramtoSend += getParam(od->templateCode)
            + getParam(od->functionCode)
            + getParam(od->functionID)
            + od->params
            + (char)lineTerminatorLen;

          send(s, paramtoSend.c_str(), paramtoSend.length(), 0);
        }

        send(s, "\0", 1, 0);

        shutdown(s, SD_SEND);

        char *buf = (char*)calloc(bufLength, sizeof(char));
        int recvLen = 1;
        string recvchars = "";
        while(recvLen > 0){
          recvLen = recv(s, buf, bufLength, 0);
          recvchars.insert(recvchars.length(), buf, recvLen);
        }

        shutdown(s, SD_RECEIVE);
        closesocket(s);
        free(buf);

        objData currentData;
        for(int i = 0; i < recvchars.length(); i++){
          //getting the template code

          bool doLoop = true;
          for(; i < recvchars.length() && doLoop; i++){
            currentData.templateCode = getParam<int>(recvchars, i);
            switch(currentData.templateCode){
              case oprMagicNum:
              case sendMagicNum:
              case reqMagicNum:
                doLoop = false;
                i += 3;
                break;
            }
          }

          //getting the code and id
          currentData.functionCode = getParam<unsigned short>(recvchars, i);
          currentData.functionID = getParam<unsigned short>(recvchars, i+2);
          i += 4;

          string paramLen = (*cbToGetParamLen)(currentData.templateCode, currentData.functionCode, ptrToClassCallback);
          for(int p_iter = 0; p_iter < paramLen.length(); p_iter++)
            for(int pLen_iter = 0; i < recvchars.length() && pLen_iter < paramLen[p_iter]; pLen_iter++)
              currentData.params += recvchars[i++];

          cerr << currentData.functionCode << endl;
          if(currentData.templateCode == oprMagicNum)
            operationalReturn(currentData);

          (*callbackFunction)(currentData, ptrToClassCallback);

          while(recvchars[i+1] != '\n')
            i++;

          if(recvchars[i+1] == '\0')
            i = recvchars.length();
        }

        delete[] currobjs;
      }
    }
    catch(runtime_error err){
      cerr << "ERR: " << err.what() << endl;
    }
    catch(logic_error err){
      cerr << "ERR: " << err.what() << endl;
    }
    catch(exception err){
      cerr << "Another error: " << err.what() << endl;
    }

    get_cv()->notify_all();
  }

  void IOSocket::inputThread(){
    while(keepSending){
      getchar();
      gettingData = true;
      thread_cv.notify_all();
    }
  }

  string dumpStringFunc(int, unsigned short, void*){
    return string("");
  }

  void dumpCallback(objData o, void* v){

  }

  IOSocket::IOSocket(){
    cbToGetParamLen = &dumpStringFunc;
    callbackFunction = &dumpCallback;
    localHostAddr.sin_addr.S_un.S_addr = inet_addr("127.0.0.1");
    localHostAddr.sin_family = AF_INET;
  }

  IOSocket::~IOSocket(){
    /*keepSending = false;
    thread_cv.notify_all();
    sockethandler->join();*/
  }

  void IOSocket::startSocket(){
    sockethandler = new thread(&IOSocket::socketThread, this);
    inputhandler = new thread(&IOSocket::inputThread, this);
  }

  void IOSocket::stopSocket(){
    keepSending = false;
    thread_cv.notify_all();
  }

  void IOSocket::queueData(objData dat){
    lock_guard<mutex> lg(objQueue_m);
    objQueue.insert(objQueue.end(), dat);

    if(isWaitingForData){
      thread_cv.notify_all();
      isWaitingForData = false;
    }
  }
  
#endif