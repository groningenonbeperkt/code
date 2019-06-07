#!/bin/bash
set -e

## Used environment variables
# INTERVAL_BETWEEN_CHECKS: The interval between checks for an update
# CURRENT_PBF_DOWNLOAD: The url for downloading the initial file
# CURRENT_PBF_STATE_DOWNLOAD: The url for the initial state file
# UPDATE_PBF_STATE_DOWNLOAD: The url for the updated states

if [[ -z "${INTERVAL_BETWEEN_CHECKS}" ]]; then
  export INTERVAL_BETWEEN_CHECKS=3600
fi

if [[ -z "${CURRENT_PBF_DOWNLOAD}" ]]; then
  export CURRENT_PBF_DOWNLOAD="http://download.geofabrik.de/europe/netherlands-latest.osm.pbf"
fi

if [[ -z "${CURRENT_PBF_STATE_DOWNLOAD}" ]]; then
  export CURRENT_PBF_STATE_DOWNLOAD="http://download.geofabrik.de/europe/netherlands-updates/state.txt"
fi

if [[ -z "${UPDATE_PBF_STATE_DOWNLOAD}" ]]; then
  export UPDATE_PBF_STATE_DOWNLOAD="http://download.geofabrik.de/europe/netherlands-updates/"
fi

# Initialize timestamp with day of latest planet dump
# Setting to midnight ensures we get conistent data after first run
# osmosis --read-replication-interval-init

if [ ! -f /osrm_data/current.osm.pbf ]; then
	echo "Initialize data"
	
	mkdir -p /osrm_data
	mkdir -p /osrm_data/5000
	mkdir -p /osrm_data/5001
	wget -O /osrm_data/current.osm.pbf $CURRENT_PBF_DOWNLOAD
	
	wget -O /osrm_data/state.txt $CURRENT_PBF_STATE_DOWNLOAD
	
	ln -s /osrm_data/current.osm.pbf /osrm_data/5000/current.osm.pbf
	ln -s /osrm_data/current.osm.pbf /osrm_data/5001/current.osm.pbf
	
	cat > /osrm_data/configuration.txt <<EOL
	# The URL of the directory containing change files.
baseUrl=${UPDATE_PBF_STATE_DOWNLOAD}

# Defines the maximum time interval in seconds to download in a single invocation.
# Setting to 0 disables this feature.
# 24h
maxInterval = ${INTERVAL_BETWEEN_CHECKS}
EOL

	#osrm-extract /osrm_data/5000/current.osm.pbf -p /osrm_scripts/wheelchair.lua
	osrm-extract /osrm_data/5000/current.osm.pbf -p /usr/local/share/osrm/profiles/car.lua
	osrm-contract /osrm_data/5000/current.osrm
	
	supervisorctl start osrm5000
	
	echo "Initialization done"
fi

sleep 5

# Check that either port 5000 or 5001 is used. Otherwise start the process on port 5000
if ! netstat -an | grep LISTEN | grep -v ^unix | grep :5000 ; then
	if ! netstat -an | grep LISTEN | grep -v ^unix | grep :5001 ; then
		echo "Start osrm5000"
		supervisorctl start osrm5000
	fi
fi

OSMOSIS_PATH=/usr
OSMOSIS_WORKDIR=/osrm_data
OSM2PGSQL_PATH=/usr/bin

# Read in current state
. $OSMOSIS_WORKDIR/state.txt

while (true)
do
	echo "Check for changes"
    file="changes-${sequenceNumber}.osm.gz"

    # Osmosis sometimes returns an error when the server is temporarily unavailable
    # If this happens, retry in a few minutes
    set +e
    $OSMOSIS_PATH/bin/osmosis \
        --read-replication-interval workingDirectory=$OSMOSIS_WORKDIR \
        --simc \
        --write-xml-change file="${file}" compressionMethod="gzip"
    if [ $? -eq 0 ]; then
        set -e
        prevSequenceNumber=$sequenceNumber
        # Read in new state
        . $OSMOSIS_WORKDIR/state.txt

        if [ "${sequenceNumber}" == "${prevSequenceNumber}" ]; then
            echo "No new data available. Sleeping..."
            # Remove file, it will just be an empty changeset
            rm ${file}
            sleep $INTERVAL_BETWEEN_CHECKS
        else
            #PGPASSWORD=$DATABASE_PASSWORD
            #echo "Fetched new data from ${prevSequenceNumber} to ${sequenceNumber} into ${file}"
            #$OSM2PGSQL_PATH/osm2pgsql \
            #   --port 5432 \
            #   --user $DATABASE_USERNAME \
            #   --slim \
            #   --append \
            #   -H localhost \
            #  -d $DATABASE \
            #   ${file}
			
			echo "Apply new changes"
            
			osmium apply-changes -o /osrm_data/new.current.pbf ${file}
			
			if [ $? -eq 0 ]; then
				mv /osrm_data/new.current.pbf /osrm_data/current.pbf
				
				if netstat -an | grep LISTEN | grep -v ^unix | grep :5000 ; then
					#osrm-extract /osrm_data/5001/current.osm.pbf -p /osrm_scripts/wheelchair.lua
					osrm-extract /osrm_data/5001/current.osm.pbf -p /usr/local/share/osrm/profiles/car.lua
					osrm-contract /osrm_data/5001/current.osrm
					
					supervisorctl start osrm5001
					supervisorctl stop osrm5000
					
					echo "Start osrm5001"
				else
					#osrm-extract /osrm_data/5000/current.osm.pbf -p /osrm_scripts/wheelchair.lua
					osrm-extract /osrm_data/5000/current.osm.pbf -p /usr/local/share/osrm/profiles/car.lua
					osrm-contract /osrm_data/5000/current.osrm
					
					supervisorctl start osrm5000
					supervisorctl stop osrm5001
					
					echo "Start osrm5000"
				fi
			fi
        fi
        # Delete old downloads
        find . -name 'changes-*.gz' -mmin +300 -exec rm -f {} \;
    else
        set -e
        echo "Waiting a few minutes before retry"
        sleep 300
    fi
done
