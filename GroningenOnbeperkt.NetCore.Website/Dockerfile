# # Sample contents of Dockerfile
# # Stage 1
# FROM microsoft/aspnetcore-build:2.0 AS builder
# RUN mkdir -p /source
# WORKDIR /source
# WORKDIR /source
#
# # caches restore result by copying csproj file separately
# #COPY *.csproj .
# #RUN dotnet restore
#
# # copies the rest of your code
# COPY . .
# #RUN pwd
# #RUN ls
# WORKDIR /source/GroningenOnbeperkt.NetCore.Website
# #RUN dotnet restore
# RUN dotnet publish --output /app/ --configuration Release
#
# # Stage 2
# FROM microsoft/aspnetcore:2.0
# WORKDIR /app
# COPY --from=builder /app .
# 
#EXPOSE 80
#
#RUN chmod +x ./entrypoint.sh
#CMD /bin/bash ./entrypoint.sh

FROM microsoft/aspnetcore-build:2.0.3
COPY . /build
WORKDIR /build
RUN ["dotnet", "restore", "GroningenOnbeperkt.NetCore.Website"]
RUN ["dotnet", "build", "GroningenOnbeperkt.NetCore.Website"]
EXPOSE 80/tcp
RUN dotnet publish GroningenOnbeperkt.NetCore.Website --output /app/ --configuration Release
WORKDIR /app
RUN chown -R www-data:www-data .
RUN chmod +x ./entrypoint.sh
RUN apt-get update && apt-get install dos2unix
RUN dos2unix entrypoint.sh
CMD /bin/bash ./entrypoint.sh

