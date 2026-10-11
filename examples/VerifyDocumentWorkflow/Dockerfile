# Build from the repository root: docker build -f examples/VerifyDocumentWorkflow/Dockerfile .
ARG SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0-noble
ARG RUNTIME_IMAGE=mcr.microsoft.com/dotnet/runtime:8.0-bookworm-slim
FROM ${SDK_IMAGE} AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY src/ ./src/
COPY docs/ ./docs/
COPY LICENSE.txt ./
COPY tests/TestFiles/font/NotoSans-Regular.ttf ./tests/TestFiles/font/NotoSans-Regular.ttf
COPY examples/GenerateInvoices/OFL.txt ./examples/GenerateInvoices/OFL.txt
COPY examples/VerifyDocumentWorkflow/ ./examples/VerifyDocumentWorkflow/
RUN dotnet publish examples/VerifyDocumentWorkflow/VerifyDocumentWorkflow.csproj --configuration Release --output /publish

FROM ${RUNTIME_IMAGE} AS runtime
ARG TESSERACT_VERSION=
ARG ENG_DATA_VERSION=
ARG OSD_DATA_VERSION=
USER root
RUN apt-get update \
    && if [ -n "$TESSERACT_VERSION" ]; then TESSERACT_PACKAGE="tesseract-ocr=$TESSERACT_VERSION"; else TESSERACT_PACKAGE=tesseract-ocr; fi \
    && if [ -n "$ENG_DATA_VERSION" ]; then ENG_PACKAGE="tesseract-ocr-eng=$ENG_DATA_VERSION"; else ENG_PACKAGE=tesseract-ocr-eng; fi \
    && if [ -n "$OSD_DATA_VERSION" ]; then OSD_PACKAGE="tesseract-ocr-osd=$OSD_DATA_VERSION"; else OSD_PACKAGE=tesseract-ocr-osd; fi \
    && apt-get install -y --no-install-recommends "$TESSERACT_PACKAGE" "$ENG_PACKAGE" "$OSD_PACKAGE" \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /publish/ ./
RUN mkdir /out \
    && chown 1654:1654 /out \
    && dpkg-query -W -f='${Package}\t${Version}\n' > /app/ocr-packages.tsv \
    && find /usr/share/tesseract-ocr -type f \( -name eng.traineddata -o -name osd.traineddata \) -exec sha256sum {} \; > /app/tessdata.sha256 \
    && find /app/runtimes/linux-x64/native -maxdepth 1 -type f -name libpdfium.so -exec sha256sum {} \; > /app/pdfium.sha256 \
    && /usr/bin/tesseract --version > /app/tesseract-version.txt 2>&1 \
    && /usr/bin/tesseract --list-langs > /app/tesseract-languages.txt 2>&1 \
    && test -s /app/tessdata.sha256 \
    && test "$(wc -l < /app/tessdata.sha256)" -eq 2 \
    && test -s /app/pdfium.sha256
ENV HOME=/tmp \
    DOTNET_EnableDiagnostics=0 \
    TESSDATA_PREFIX=/usr/share/tesseract-ocr/5/tessdata
USER 1654:1654
ENTRYPOINT ["/usr/bin/timeout", "--signal=TERM", "--kill-after=5s", "60s", "dotnet", "VerifyDocumentWorkflow.dll"]
CMD ["/out"]
