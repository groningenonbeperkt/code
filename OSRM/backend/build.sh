#!/bin/sh
# profile url (alternative imageName)
START="/home/ealse/testdockerfile"
if [ -n "$3" ]; then START="$3";fi
cd $START

NAME="osrm"
if [ -n "$4" ]; then NAME="$4";fi
echo "Name of image is $NAME"

DATANAME=${2##*/}
DATANAME=${DATANAME%%.*}

MAPNAMEOLD="UpToDateVersion"
MAPNAMENEW="ReplaceVersion"

CURRENTVERSION=$MAPNAMEOLD

[ ! -z $(docker images -q NAME) ] || $(docker build -t $NAME .)

if [ -d $MAPNAMEOLD ];
 then  #if exist
   [ ! -d $MAPNAMENEW ] || $(rm -r $MAPNAMENEW)
   mkdir $MAPNAMENEW
   CURRENTVERSION=$MAPNAMENEW
else
   mkdir $MAPNAMEOLD
fi

wget $2 -P "$(pwd)/$CURRENTVERSION"

docker run -t -v $(pwd):/data $NAME osrm-extract -p /opt/$1.lua /data/$CURRENTVERSION/$DATANAME.osm.pbf
docker run -t -v $(pwd):/data $NAME osrm-contract /data/$CURRENTVERSION/$DATANAME.osrm

if [ "$CURRENTVERSION" = "$MAPNAMENEW" ]; then
  docker stop $MAPNAMEOLD
  docker rm $MAPNAMEOLD
  rm -r $MAPNAMEOLD
  mv  $(pwd)/$MAPNAMENEW $(pwd)/$MAPNAMEOLD 
fi

docker run -d --name $MAPNAMEOLD -p 5000:5000 -v $(pwd):/data $NAME osrm-routed /data/$MAPNAMEOLD/$DATANAME.osrm
