#ifndef COREDLL_H
#define COREDLL_H

#ifdef BUILD_DLL
  #define DLLLIB __declspec(dllexport)
#else
  #define DLLLIB __declspec(dllimport)
#endif

#define oprMagicNum 0x0b6f7072
#define sendMagicNum 0x0b534e44
#define reqMagicNum 0x0b524551
#define lineTerminatorLen (int)'\n'
#define funcInfoCode 8


#include <map>
#include <iostream>
#include <vector>
#include <math.h>
#include <thread>
#include <mutex>
#include <queue>
#include <exception>
#include <condition_variable>
using namespace std;

struct objData{
  unsigned int templateCode = 0;
  unsigned short functionCode = 0, functionID = 0;
  string params = "";
};

#ifdef _WIN32
  #include <winsock2.h>
  #include <ws2tcpip.h>
  #include <windows.h>

  class IOSocket{
    private:
      condition_variable thread_cv;
      mutex objQueue_m;
      vector<objData> objQueue;
      thread *sockethandler, *inputhandler;
      bool isWaitingForData = true, keepSending = true, gettingData = false;
      sockaddr_in localHostAddr;

      void socketThread();
      void inputThread();

    public:
      void* ptrToClassCallback;
      //for passing objdata after getting one
      void (*callbackFunction)(objData, void*);
      //for taking a string of param lenght
      string (*cbToGetParamLen)(int, unsigned short, void*);

      //using localhost
      IOSocket();
      ~IOSocket();
      void startSocket();
      void stopSocket();
      void queueData(objData obj);
  };

  void initSocket(unsigned short port, unsigned short pid);
  void closeSocket();
#endif

template<class primitiveType> primitiveType StrToNum(string str){
  primitiveType res = 0;
  for(size_t i = 0; i < str.length(); i++){
    if(str[i] >= '0' && str[i] <= '9')
      res += (str[i] - '0') * (int)pow(10, (str.length() - (i + 1)));
  }

  if(str[0] == '-')
    res *= -1;
  
  return res;
}


template<class primitiveType> class DLLLIB vec2{
  public:
    primitiveType x, y;

    vec2(primitiveType x, primitiveType y);
    vec2<primitiveType> operator+(vec2<primitiveType> v);
    vec2<primitiveType> operator-(vec2<primitiveType> v);
    void operator+=(vec2<primitiveType> v);
    void operator-=(vec2<primitiveType> v);
    primitiveType dotProduct(vec2<primitiveType> v2);
    static primitiveType dotProduct(vec2<primitiveType> &v1, vec2<primitiveType> &v2);
    double magnitude();
};

template class vec2<int>;
template class vec2<long>;
template class vec2<float>;
template class vec2<double>;

template<class primitiveType> DLLLIB ostream &operator<<(ostream &o, vec2<primitiveType> &v){
  o << "(x: " << v.x << ", y: " << v.y << ")";
  return o;
}


template<class toType> toType getParam(string str, size_t at){
  size_t typeSize = sizeof(toType);
  toType res{};
  char *typePointer = reinterpret_cast<char*>(&res);
  
  for(int i = 0; at < str.length() && i < typeSize; i++){
    typePointer[i] = str[at];
    at++;
  }

  return res;
}


template<class toType> string getParam(toType t){
  string res{};
  res.reserve(sizeof(toType));
  char *typePointer = reinterpret_cast<char*>(&t);

  for(int i = 0; i < sizeof(t); i++)
    res += typePointer[i];

  return res;
}


template<class fromType> void dumpToStdout(fromType t){
  for(int i = 0; i < sizeof(fromType); i++)
    cout << (char)(t >> ((sizeof(fromType)-1)-i)*8);
}


DLLLIB void initialize(int argi, char **argc);
DLLLIB void close();
DLLLIB condition_variable *get_cv();

void operationalReturn(objData &obj);
void initializeFunc();
void closeFunc();


/** Mathematical Functions **/

  class RandomizerClass{
    public:
      static void initializerandom();
      static int getrandom();
  };

#endif