#!/bin/sh

ID3TEST="./id3test.exe -v"

${ID3TEST} "$1"

case "$?" in

0)
  exit 0;
  ;;
2)
  echo "Converting $1"
  ;;

*)
  echo "id3test unexpected error"
  exit $?;
  ;;

esac
