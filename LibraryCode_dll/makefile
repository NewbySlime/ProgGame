main: compile_all link_all compile_main
	@echo done compiling!

link_all:
	$(CXX) *.o -o coredll.dll -shared -lws2_32 -lmswsock -ladvapi32

compile_all:
	$(CXX) -c *.cpp -DBUILD_DLL -Wall

compile_main:
	$(CXX) -c ./mainscript/main.cpp -o ./mainscript/main.o