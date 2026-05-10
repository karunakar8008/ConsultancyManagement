# syntax=docker/dockerfile:1
FROM node:22-alpine AS ui-build
WORKDIR /ui
COPY consultancy-management-ui/package.json consultancy-management-ui/package-lock.json ./
RUN npm ci
COPY consultancy-management-ui/ ./
RUN npm run build -- --configuration production-docker

FROM nginx:1.27-alpine
COPY nginx/consultancy-fullstack.conf /etc/nginx/conf.d/default.conf
COPY --from=ui-build /ui/dist/consultancy-management-ui/browser /usr/share/nginx/html
