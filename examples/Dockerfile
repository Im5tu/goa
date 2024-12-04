# Stage 1 : Build the artifact
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG PROJECT
ENV PROJECT=$PROJECT
WORKDIR /app
COPY . .
RUN cd ${PROJECT} && dotnet restore -r linux-x64 && dotnet publish --no-restore -c Release -o /app/publish

# Stage 2 : Copy to Amazon Linux 2023 for Lambda
FROM public.ecr.aws/lambda/provided:al2023 AS runtime
# Images seem to fail without this. Not sure why...
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
COPY --from=build /app/publish /var/runtime
RUN chmod +x /var/runtime/bootstrap
CMD ["/var/runtime/bootstrap"]

# docker build --no-cache --build-arg PROJECT=SampleApi -t test .
# docker run -it --rm -p 9000:8080 test
