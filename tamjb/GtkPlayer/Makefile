# $Id$

DEBUGFLAGS = -g -define:DEBUG -define:TRACE

SRCS = Main.cs \
        GtkPlayer.cs \
        ConfigWindow.cs \
        DatabaseConfigWindow.cs \
        MiscSettings.cs \
        MoodWindow.cs \
        PlayerSettings.cs

DEPLIBS = ../Interfaces/tamjb.Interfaces.dll


PLAYER_REFS = -r System.Data \
        -r System.Runtime.Remoting \
        $(addprefix -r:, $(DEPLIBS)) \
        -pkg:gtk-sharp \
        -pkg:glade-sharp

RESOURCES = -resource:tam.GtkPlayer.exe.glade

all: tamjb.GtkPlayer.exe 

tamjb.GtkPlayer.exe: $(SRCS) $(DEPLIBS) tam.GtkPlayer.exe.glade Makefile
	mcs $(DEBUGFLAGS) $(LIBPATH) $(PLAYER_REFS) $(RESOURCES) \
                -out:$@ $(SRCS)

