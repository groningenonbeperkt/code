#!/usr/bin/bash

if psql -lqt | cut -d \| -f 1 | grep -qw $DATABASE; then
    
else
    psql -c "CREATE USER ${DATABASE_USERNAME} WITH PASSWORD '${DATABASE_PASSWORD}'"
	psql -c "CREATE DATABASE ${DATABASE} WITH OWNER ${DATABASE_USERNAME}"
	psql -c "GRANT ALL ON DATABASE ${DATABASE} TO ${DATABASE_USERNAME}"
	
	psql -c "CREATE EXTENSION postgis; CREATE EXTENSION postgis_topology;" $DATABASE
	
	# http://download.geofabrik.de/europe/netherlands-latest.osm.pbf
	wget -O /data/download.osm.pbf $DOWNLOAD_URL
	
	# http://download.geofabrik.de/europe/netherlands-updates/state.txt
	wget -O /data/state.txt $INITIAL_STATE_DOWNLOAD_URL
	
	JAVACMD_OPTIONS=-server -Xmx6G
	
	# osmosis --read-pbf /home/geodude/download.osm.pbf --log-progress --write-pgsimp-dump directory=/data/
	osmosis --read-pbf /data/download.osm.pbf --log-progress --write-pgsimp database=$DATABASE user=$DATABASE_USERNAME password=$DATABASE_PASSWORD host=127.0.0.1
fi