FROM node:22-bookworm-slim AS frontend-build
WORKDIR /src/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
ARG VITE_DEMO_MODE=true
ENV VITE_DEMO_MODE=${VITE_DEMO_MODE}
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api-build
WORKDIR /src
COPY backend/ ./backend/
RUN dotnet publish backend/src/LegacyModernization.Api/LegacyModernization.Api.csproj \
    --configuration Release \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS runtime
ENV DEBIAN_FRONTEND=noninteractive
RUN apt-get update \
    && apt-get install --yes --no-install-recommends python3 python3-pip python3-venv \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY python/requirements.txt /tmp/requirements.txt
RUN python3 -m venv /opt/venv \
    && /opt/venv/bin/pip install --no-cache-dir --upgrade pip \
    && /opt/venv/bin/pip install --no-cache-dir -r /tmp/requirements.txt
COPY --from=api-build --chown=app:app /app/publish/ ./
COPY --from=frontend-build --chown=app:app /src/frontend/dist/ ./wwwroot/
COPY --chown=app:app python/ ./python/
COPY --chown=app:app samples/LegacySampleProject/ ./samples/LegacySampleProject/
COPY --chown=app:app samples/LegacySampleProject.Tests/ ./samples/LegacySampleProject.Tests/
RUN mkdir -p /app/data && chown app:app /app/data
ENV ASPNETCORE_URLS=http://+:7860 \
    ASPNETCORE_ENVIRONMENT=Production \
    DEMO_MODE=true \
    PYTHON=/opt/venv/bin/python \
    PYTHONUNBUFFERED=1 \
    RAG_STORE_MODE=sqlite \
    RAG_SQLITE_PATH=/app/data/.legacy_rag.sqlite3 \
    PATH=/opt/venv/bin:$PATH
USER app
EXPOSE 7860
ENTRYPOINT ["dotnet", "LegacyModernization.Api.dll"]