# $Id$

LIBDIR = ../../lib

DEBUGFLAGS = -g -define:DEBUG -define:TRACE

SRCS = ID3v2.cs \
        ID3v2Header.cs \
        ID3v2FrameHeader.cs \
        ID3v2TagNotFoundException.cs \
        Mp3StreamReader.cs

TARGET = id3.dll

CMDLINE = id3test.exe

all: $(TARGET) $(CMDLINE)

$(TARGET): $(SRCS) Makefile
	mcs $(DEBUGFLAGS) $(LIBPATH) -target:library -out:$@ $(SRCS)


$(CMDLINE): $(TARGET) main.cs Makefile
	mcs $(DEBUGFLAGS)  -r $(TARGET) -out:$@ main.cs 