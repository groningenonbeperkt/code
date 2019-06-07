#!/bin/bash
set -e

# Initialize timestamp with day of latest planet dump
# Setting to midnight ensures we get conistent data after first run
# osmosis --read-replication-interval-init

if [ -e $CURRENT_PBF_FILE ]; then
	wget -o $CURRENT_PBF_FILE $CURRENT_PBF_DOWNLOAD
	
	
fi

OSMOSIS_PATH=/usr
OSMOSIS_WORKDIR=/osm_updates
OSM2PGSQL_PATH=/usr/bin

# Read in current state
. $OSMOSIS_WORKDIR/state.txt

while (true)
do
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
            
			osmium apply-changes -o $NEW_PBF_FILE $CURRENT_PBF_FILE ${file}
			
			if [ $? -eq 0 ]; then
				rm $CURRENT_PBF_FILE
				mv $NEW_PBF_FILE $CURRENT_PBF_FILE
				osrm-extract $CURRENT_PBF_FILE -p $WHEELCHAIR_PROFILE
				osrm-contract $CURRENT_PBF_FILE_OSRM
				
				if lsof -Pi :5000 -sTCP:LISTEN -t >/dev/null ; then
					echo "running"
				else
					echo "not running"
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
