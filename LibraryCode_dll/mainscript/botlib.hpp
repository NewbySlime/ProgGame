#ifndef BOTLIB_HEADER
#define BOTLIB_HEADER

#define DLLLIB __declspec(dllimport)

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

void DLLLIB moveTo(float x, float y);
void DLLLIB turnBy(float degrees);

vec2<float> DLLLIB currentPosition();
float DLLLIB currentAngleDeg();

#endif