From ubuntu:latest

RUN apt-get update && apt-get upgrade -y
RUN apt-get install -y build-essential git cmake pkg-config libbz2-dev libstxxl-dev libstxxl1v5 libxml2-dev libzip-dev libboost-all-dev lua5.2 liblua5.2-dev libtbb-dev
RUN apt-get install -y osmosis osmium-tool
RUN apt-get install -y supervisor wget liblua5.2-0 libboost-all-dev libtbb2 net-tools

RUN mkdir -p /opt/osrm
WORKDIR /opt/osrm/
RUN git clone https://github.com/Project-OSRM/osrm-backend.git
WORKDIR /opt/osrm/osrm-backend
RUN git checkout 5.13

RUN mkdir -p build
WORKDIR /opt/osrm/osrm-backend/build
RUN cmake .. -DCMAKE_BUILD_TYPE=Release
RUN cmake --build .
RUN cmake --build . --target install

RUN apt-get purge -y build-essential git cmake pkg-config libbz2-dev libstxxl-dev libstxxl1v5 libxml2-dev libzip-dev lua5.2 liblua5.2-dev libtbb-dev
RUN apt-get autoremove -y

RUN rm -fR /opt/osrm

RUN mkdir -p /osrm_data
RUN chown www-data:www-data /osrm_data
RUN mkdir -p /osrm_scripts
COPY wheelchair.lua /osrm_scripts/wheelchair.lua
COPY osrm-supervisor.conf /etc/supervisor/conf.d/
COPY update_osrm_data.sh /osrm_scripts/update_osrm_data.sh
COPY entrypoint.sh /osrm_scripts/entrypoint.sh
RUN chmod 755 /osrm_scripts/update_osrm_data.sh
RUN chmod 755 /osrm_scripts/entrypoint.sh
RUN update-rc.d supervisor defaults
RUN apt-get update && apt-get install dos2unix
RUN dos2unix /osrm_scripts/entrypoint.sh
CMD /bin/bash /osrm_scripts/entrypoint.sh