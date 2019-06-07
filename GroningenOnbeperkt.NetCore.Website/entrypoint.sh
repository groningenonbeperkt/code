#!/bin/bash

#sleep infinity & wait

set -e
cd /build/GroningenOnbeperkt.NetCore.Website
run_cmd="dotnet GroningenOnbeperkt.NetCore.Website.dll"

until dotnet ef database update; do
>&2 echo "Mysql Server is starting up"
sleep 1
done

cd /app

>&2 echo "Mysql Server is up - executing command"
exec $run_cmd