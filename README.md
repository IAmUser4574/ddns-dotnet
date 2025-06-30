# ddns-dotnet

## Introduction
* `ddns-dotnet` is a simple project for updating Cloudflare A records with the runtime machine's latest IPV4 if it changes
  * The typical use case for a DDNS service is hosting on a dynamic IP network (such as in residential internet)
* This project utilizes public APIs to determine the host IPV4


## Use
* Clone the repo
* Update either the `appsettings.json` file or the `docker-compose.yml`'s environment section with your Cloudflare email and api key
  * Note that Cloudflare API *Key* is supported currently and not scoped Tokens 
* Start and stop with compose:
  * `docker compose up -d`
  * `docker compose down`
* Logs can be tailed from the host either by file in the `logs/` directory or by stdout via `docker logs ddns-dotnet --follow` 
