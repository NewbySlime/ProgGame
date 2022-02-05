#ifndef MAIN_HEADER
#define MAIN_HEADER

#define DLLLIB __declspec(dllimport)

#include <thread>
#include <condition_variable>
#include <mutex>

using namespace std;

DLLLIB void initialize(int argi, char **argc);
DLLLIB void close();
DLLLIB condition_variable *get_cv();

void start();

#endif