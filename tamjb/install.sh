#!/bin/sh

# A helper script to register those dll's on mono platforms

if [ x == x${PREFIX} ] ; then
  PREFIX=/usr/local/bin
fi

if [ x == x${INSTALLDIR} ] ; then
  INSTALLDIR=/usr/local/lib/tamjb
fi

gui_wrapper=gtktamjb

# These are installed in (like) /usr/local/lib/tamjb

install_files="tamjb.Engine.dll tamjb.Interfaces.dll id3helper.exe tamjb.GtkPlayer.exe tamjb.Server.exe"

# These are/were copied to the gac

lib_dlls="Mp3Sharp.dll byteheaven.id3.dll esd-sharp.dll tamjb.SimpleMp3Player.dll "

#for dll in $lib_dlls ; do
#  echo "Registering ${dll}"
#  gacutil -i bin/${dll} -check_refs || exit 1
#done

install -d "${INSTALLDIR}" || exit 1
for exe in $install_files $lib_dlls ; do
  echo "Installing bin/${exe}"
  install "bin/${exe}" "${INSTALLDIR}/" || exit 1
done

install "$gui_wrapper" "${PREFIX}/" || exit 1

