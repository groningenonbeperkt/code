# Groningen Onbeperkt
Groningen Onbeperkt was ooit bedoeld als een routeplanner voor rolstoelgebruikers. Het is alleen nooit heel ver gekomen. Daarom heb ik besloten om de basis die we hebben gemaakt online te zetten en hopelijk dat iemand het concept verder wil uitwerken.

## Code opdeling
* GroningenOnbeperkt.NetCore.Website: Algemene website
* GroniningenOnbeperkt.NetCore.TagEditor.Openstreetmap: Editen van attributen op Openstreetmap
* Microsoft.AspNetCore.Authentication.Openstreetmap: Authenticeren bij Openstreetmap voor bewerken van Openstreetmap data
* nginx: Reverse proxy voor website en OSRM routeplanner in Docker
* OSRM: OSRM routeplanner met custom profiel voor rolstoel
* Eindverslag - Groningen onbeperkt.docx: Achtergrond informatie over Groningen onbeperkt