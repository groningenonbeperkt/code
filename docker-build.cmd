dotnet restore ./Groningen onbeperkt.sln && dotnet publish ./Groningen onbeperkt.sln -c Release -o ./obj/Docker/publish
docker-compose build