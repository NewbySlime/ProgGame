#include "coredll.h"
#include "funcs_list.h"

condition_variable main_cv{};

void toBinary(string s, size_t typeSize){
  for(int i = s.length()-1 < typeSize? s.length(): typeSize; i >= 0; i--){
    unsigned char c = s[i];
    int cint = c;
    for(int i = 7; i >= 0; i--){
      int n = (int)pow(2, i);
      if(n <= cint)
        cint -= n;
    }
  }
}

void operationalReturn(objData &obj){
  switch(obj.functionCode){
    case FUNCS_PROGRAM_EXIT:
      main_cv.notify_all();
  }
}


DLLLIB void initialize(int argi, char **argc){
  if(argi < 3)
    exit(-1);

  unsigned short port, pid;
  bool port_isready = false, pid_isready = false;
  for(int i = 1; i < argi; i++){
    size_t charrlen = strlen(argc[i]);
    if(argc[i][0] == '-'){
      int offset = 1;
      while(argc[i][offset] != '=' && offset < charrlen)
        offset++;

      string opt(offset-1, '\0');
      for(int str_i = 1; str_i < offset; str_i++)
        opt[str_i-1] = argc[i][str_i];

      offset++;
      string opt_param(charrlen-offset, '\0');
      for(int str_i = offset; str_i < charrlen; str_i++)
        opt_param[str_i-offset] = argc[i][str_i];

      if(opt == "port"){
        port = StrToNum<unsigned short>(opt_param);
        port_isready = true;
      }
      else if(opt == "pid"){
        pid = StrToNum<unsigned short>(opt_param);
        pid_isready = true;
      }
    }
  }

  if(!port_isready || !pid_isready)
    exit(-1);
  
#ifdef _WIN32
  initSocket(port, pid);
#endif

  initializeFunc();
}


DLLLIB void close(){
  closeFunc();

#ifdef _WIN32
  closeSocket();
#endif
}

DLLLIB condition_variable *get_cv(){
  return &main_cv;
}