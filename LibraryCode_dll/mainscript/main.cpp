#include "main_core.h"

thread *tptr;
bool isThreadDone = false;

void _start(){
   start();
   isThreadDone = true;
   get_cv()->notify_all();
}

int main(int argi, char **argc){
   initialize(argi, argc);
   mutex m;
   unique_lock<mutex> ul(m);
   tptr = new thread(_start);
   get_cv()->wait(ul);
   if(isThreadDone)
      delete tptr;
      
   close();
}