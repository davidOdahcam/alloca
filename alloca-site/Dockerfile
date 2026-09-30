# Build
FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# Runtime (nginx serve os arquivos estáticos e faz proxy de /api para a API)
FROM nginx:alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/alloca-ng/browser /usr/share/nginx/html
EXPOSE 80
