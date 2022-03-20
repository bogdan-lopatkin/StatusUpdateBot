#!/bin/sh

if [ -z "$1" ]; then
  RUNTIME=linux-arm64
else
  RUNTIME=$1
fi

if [ "$1" == "--help" ]; then
   echo "Usage: build.sh <RUNTIME>"
   echo "Where RUNTIME must match the target machine architecture"
   exit
fi

dotnet publish -c release -r $RUNTIME --self-contained -p:PublishProfile=PublishReadyToRun -o output
rm output/appsettings.json

docker build . -t status-update-bot

rm -r output

echo "Run with docker run -it --rm -v $(pwd):/config/ status-update-bot"