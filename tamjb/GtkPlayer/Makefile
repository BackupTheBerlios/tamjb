# $Id$

LIBDIR = ../../lib

LIBPATH = -L $(LIBDIR)

DEBUGFLAGS = -g -define:DEBUG -define:TRACE

SRCS = MainWnd.cs \
        ConfigDlg.cs \
        PlayerSettings.cs

PLAYER_REFS = -r System.Data \
        -r System.Runtime.Remoting \
        -r tam.Interfaces \
        -r gtk-sharp \
        -r glib-sharp

DEPLIBS = ../../lib/tam.Interfaces.dll

all: tam.GtkPlayer.exe 

tam.GtkPlayer.exe: $(SRCS) $(DEPLIBS)
	mcs $(DEBUGFLAGS) $(LIBPATH) $(PLAYER_REFS) -out:$@ $(SRCS)

