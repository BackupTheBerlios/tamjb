#!/bin/bash
# $Id$

# A really sad script that restarts the jukebox periodically when it
# runs out of memory, then correctly kills it off when killed

PIDFILE=/var/run/tamjb.pid

# Uses bash for builtin ulimit support

# Has big memory leak--stop leak from crashing system
ulimit -m 60000
ulimit -v 90000
ulimit -a > /tmp/limit

echo $$ > $PIDFILE

# trap 'echo ${SERVERPID}; kill -TERM ${SERVERPID}; exit' TERM
# trap 'echo ${SERVERPID}; kill -TERM ${SERVERPID}; exit' INT
trap 'kill -TERM ${SERVERPID}; exit' EXIT

# Endless loop to restart the server when it runs out of memory!

while /bin/true ; do
  /var/mp3/jukebox/tamjb.Server.exe $* &
  SERVERPID=$!
  echo "LAUNCHED: ${SERVERPID}"
  wait $!

  # Sleep in case it exits instantly, to avoid evil loop
  sleep 2
done
