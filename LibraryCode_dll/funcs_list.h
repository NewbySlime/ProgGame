#ifndef FUNCS_LIST_HEADER
#define FUNCS_LIST_HEADER

#include "coredll.h"

// --- operational codes --- //
#define FUNCS_NORETURN (unsigned short)0x0000
#define FUNCS_PROGRAM_EXIT (unsigned short)0x0001

// --- send codes --- //
#define FUNCS_MOVEPOS (unsigned short)0x0000
#define FUNCS_MOVEFRONTBACK (unsigned short)0x0001
#define FUNCS_TURNDEG (unsigned short)0x0002

// --- request codes --- //
#define FUNCS_REQPOS (unsigned short)0x0000
#define FUNCS_REQANGLEDEG (unsigned short)0x0001

#define VOIDSTRING ""

// --- operational string params --- //
#define FUNCSPARAMS_RETURNVOID VOIDSTRING
#define FUNCSPARAMS_TERMINATEPROG VOIDSTRING

// --- send string params --- //
#define FUNCSPARAMS_MOVEPOS VOIDSTRING
#define FUNCSPARAMS_MOVEFRONTBACK VOIDSTRING
#define FUNCSPARAMS_TURNDEG VOIDSTRING

// --- request string params --- //
#define FUNCSPARAMS_REQPOS {(char)4, (char)4, '\0'}
#define FUNCSPARAMS_REQANDLEDEG {(char)4, '\0'}

/** send functions **/
void DLLLIB moveTo(float x, float y);
void DLLLIB turnBy(float degrees);

/** request functions **/
vec2<float> DLLLIB currentPosition();
float DLLLIB currentAngleDeg();

#endif