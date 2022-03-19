FROM ubuntu

RUN apt-get update && apt-get install -y libicu66 libssl1.1 ca-certificates --no-install-recommends && apt-get clean  && rm -rf /var/lib/apt/lists/*
COPY output /app/

WORKDIR /config

ENTRYPOINT "/app/StatusUpdateBot"
