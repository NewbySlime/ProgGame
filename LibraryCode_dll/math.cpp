#include "coredll.h"
#include "time.h"

void RandomizerClass::initializerandom(){
  srand(time(NULL));
}

int RandomizerClass::getrandom(){
  return rand();
}

template<class pT> vec2<pT>::vec2(pT x, pT y){
  this->x = x;
  this->y = y;
}

template<class pT> vec2<pT> vec2<pT>::operator+(vec2<pT> v){
  return vec2<pT>(x + v.x, y + v.y);
}

template<class pT> vec2<pT> vec2<pT>::operator-(vec2<pT> v){
  return vec2<pT>(x - v.x, y - v.y);
}

template<class pT> void vec2<pT>::operator+=(vec2<pT> v){
  x = x+v.x;
  y = y+v.y;
}

template<class pT> void vec2<pT>::operator-=(vec2<pT> v){
  x = x+v.x;
  y = y+v.y;
}

template<class pT> pT vec2<pT>::dotProduct(vec2<pT> v2){
  return vec2<pT>::dotProduct(*this, v2);
}

template<class pT> pT vec2<pT>::dotProduct(vec2<pT> &v1, vec2<pT> &v2){
  return (v1.x * v2.x) + (v1.y * v2.y);
}

template<class pT> double vec2<pT>::magnitude(){
  return pow((x*x)+(y*y), 0.5);
}