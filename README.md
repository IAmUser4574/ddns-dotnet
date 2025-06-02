# ddns-dotnet

## Introduction
* `ddns-dotnet` is a simple project aimed at quickly and efficiently updating Cloudflare A records with the runtime machine's latest ipv4 if it changes
* This project utilizes public APIs to determine the host IPV4


## Use
* Clone repo
* Update either the `appsettings.json` file or the `docker-compose.yml`'s environment section with your Cloudflare email and api key
* Start and stop with compose:
  * `docker compose up -d`
  * `docker compose down`
  * Must be in the project root directory (same level as the compose file) for these commands to work 
* Logs can be tailed from the host either by file in the `logs/` directory or by stdout via `docker logs ddns-dotnet --follow` 
