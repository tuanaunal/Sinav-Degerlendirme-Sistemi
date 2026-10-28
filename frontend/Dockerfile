# Sınav Değerlendirme Sistemi arayüzü: Vite ile derlenir, Nginx ile sunulur.
# /api istekleri Nginx tarafından API konteynerine yönlendirilir.

FROM node:22-alpine AS derleme
WORKDIR /kaynak
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:1.27-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=derleme /kaynak/dist /usr/share/nginx/html
EXPOSE 80
