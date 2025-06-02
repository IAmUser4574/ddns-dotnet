# set up base image with dotnet sdk
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# copy all files and publish application in release mode
COPY . .
RUN dotnet publish -c Release -p:PublishSingleFile=true --self-contained true  -o /app

# use the runtime image for the final container.
# --platform=$TARGETPLATFORM will select the proper variant (Linux or Windows) based on the target.
FROM --platform=$TARGETPLATFORM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app

# copy the published output from the build stage.
COPY --from=build /app .

# set the entrypoint to run application
ENTRYPOINT ["./ddns-dotnet"]