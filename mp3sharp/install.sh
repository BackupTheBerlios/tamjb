#!/bin/sh

# A helper script to register those dll's on mono platforms

if [ x == x${PREFIX} ] ; then
  PREFIX=/usr/local/bin
fi

for dll in bin/*.dll ; do
  echo "Registering ${dll}"
  gacutil -i ${dll} -check_refs || exit 1
done

for exe in bin/*.exe ; do
  echo "Installing ${exe}"
  install "${exe}" "${PREFIX}/"
done
