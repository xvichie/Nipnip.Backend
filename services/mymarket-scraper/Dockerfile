FROM ghcr.io/puppeteer/puppeteer:23.11.1

WORKDIR /usr/src/app
USER root

# The base image already ships a matching Chrome build in puppeteer's cache dir —
# skip re-downloading one when `puppeteer` installs as a dependency below.
ENV PUPPETEER_SKIP_DOWNLOAD=true

COPY package*.json ./
RUN npm ci --omit=dev

COPY . .
RUN chown -R pptruser:pptruser /usr/src/app
USER pptruser

ENV PORT=8080
EXPOSE 8080
CMD ["node", "server.js"]
