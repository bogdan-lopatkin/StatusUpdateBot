Project setup

1) Setup Google Cloud Console App and retrieve credentials https://developers.google.com/sheets/api/quickstart/dotnet
2) Copy replace credentials.json with your credentials
3) Set needed keys in appsettings.json

Creating a docker image:

`./build.sh <RUNTIME>`
`docker save status-update-bot -o status-update-bot.tar`

Run with 
`docker run -it --rm -v $(pwd):/config/ status-update-bot`
