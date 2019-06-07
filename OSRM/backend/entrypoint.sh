#!/bin/bash
set -e

/etc/init.d/supervisor start

echo "Press any key to end the process"
sleep infinity & wait