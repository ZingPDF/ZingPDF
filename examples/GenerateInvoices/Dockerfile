# Build from the repository root: docker build -f examples/GenerateInvoices/Dockerfile .
# Supply approved image digests and an apt package version for a release image.
ARG SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0-noble
ARG RUNTIME_IMAGE=mcr.microsoft.com/dotnet/runtime:8.0-bookworm-slim
FROM ${SDK_IMAGE} AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY src/ ./src/
COPY docs/ ./docs/
COPY LICENSE.txt ./
COPY tests/TestFiles/font/NotoSans-Regular.ttf ./tests/TestFiles/font/NotoSans-Regular.ttf
COPY examples/GenerateInvoices/ ./examples/GenerateInvoices/
RUN dotnet publish examples/GenerateInvoices/GenerateInvoices.csproj --configuration Release --output /publish

FROM ${RUNTIME_IMAGE} AS runtime
ARG CHROMIUM_VERSION=
USER root
RUN apt-get update \
    && if [ -n "$CHROMIUM_VERSION" ]; then \
         apt-get install -y --no-install-recommends "chromium=$CHROMIUM_VERSION" "chromium-common=$CHROMIUM_VERSION"; \
       else apt-get install -y --no-install-recommends chromium; fi \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /publish/ ./
RUN mkdir /out \
    && chown 1654:1654 /out \
    && dpkg-query -W > /app/browser-packages.txt
ENV ZINGPDF_BROWSER_PATH=/usr/bin/chromium \
    HOME=/tmp \
    DOTNET_EnableDiagnostics=0
# --no-sandbox is not a default. The verification wrapper opts in only for its isolated worker.
USER 1654:1654
ENTRYPOINT ["dotnet", "GenerateInvoices.dll"]
CMD ["/out"]
