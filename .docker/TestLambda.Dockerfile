FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app
COPY . .
RUN dotnet publish tests/TestConsole/TestConsole.csproj /p:GeneratePackageOnBuild=false

FROM public.ecr.aws/lambda/provided:al2023
COPY --from=build /app/artifacts/publish/TestConsole/release_linux-x64 /var/runtime
RUN chmod +x /var/runtime/bootstrap
CMD ["/var/runtime/bootstrap"]

# docker run -it --rm --entrypoint bash public.ecr.aws/lambda/provided:al2023
# docker run -it --rm -e GOA__LOG__LEVEL="Trace" -v ${pwd}:/var/runtime -p 9000:8080 public.ecr.aws/lambda/provided:al2023
# docker build -f .docker/TestLambda.Dockerfile -t lambda .
# docker run -it --rm --entrypoint bash lambda
# docker run -it --rm -e GOA__LOG__LEVEL="Trace" -p 9000:8080 lambda
