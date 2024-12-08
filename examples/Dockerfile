# Stage 1 : Build the artifact using an aot image
FROM mcr.microsoft.com/dotnet/nightly/sdk:9.0-noble-aot AS build
ARG PROJECT
ENV PROJECT=$PROJECT
WORKDIR /app
COPY . .
RUN ls
RUN cd ${PROJECT} && dotnet restore -r linux-x64 && dotnet publish -r linux-x64 --no-restore -c Release -o /app/publish

# Stage 2 : Copy to a final image Lambda
FROM mcr.microsoft.com/dotnet/runtime-deps:9.0-noble-chiseled AS runtime
COPY --from=build /app/publish /var/runtime
CMD ["/var/runtime/bootstrap"]

# docker build --no-cache --build-arg PROJECT=SampleApi -t test .
# docker run -it --rm -p 9000:8080 test
