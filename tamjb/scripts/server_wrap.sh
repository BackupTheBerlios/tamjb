#!/bin/bash
# $Id$
#
# Note: Uses bash for builtin ulimit support
#
# A really sad script that restarts the jukebox periodically when it
# runs out of memory, then correctly kills it off when killed

PIDFILE=/var/run/tamjb.pid

set -e

# Early versions of mono leaked memory like nobody's business.
# Set limits.
ulimit -m 80000
ulimit -v 90000
ulimit -a 1>&2

# echo $$ > $PIDFILE

# trap 'echo ${SERVERPID}; kill -TERM ${SERVERPID}; exit' TERM
# trap 'echo ${SERVERPID}; kill -TERM ${SERVERPID}; exit' INT
# trap 'kill -TERM ${SERVERPID}; exit' EXIT

# Endless loop to restart the server when it runs out of memory!

# while /bin/true ; do
  /usr/bin/mono --optimize=all /usr/local/bin/tamjb.Server.exe "$@" &
  echo $! > $PIDFILE
#  wait $!

  # Sleep in case it exits instantly, to avoid evil loop
#   sleep 2
# done

