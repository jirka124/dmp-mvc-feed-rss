# Deploy on VPS (Docker + Caddy + HTTPS)

This setup runs the app behind Caddy with automatic HTTPS.

## 1. Prepare DNS

Create an `A` record:

- `dmp.gs-empire.com` -> `<YOUR_VPS_PUBLIC_IP>`

Wait until DNS is propagated.

## 2. Install Docker on Ubuntu VPS

```bash
sudo apt-get update
sudo apt-get install -y ca-certificates curl gnupg
sudo install -m 0755 -d /etc/apt/keyrings
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg
sudo chmod a+r /etc/apt/keyrings/docker.gpg
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
```

Optional (avoid `sudo` for docker commands):

```bash
sudo usermod -aG docker $USER
newgrp docker
```

## 3. Open firewall ports

```bash
sudo ufw allow OpenSSH
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw --force enable
```

## 4. Deploy app

```bash
git clone <YOUR_REPO_URL>
cd dmp-mvc-feed-rss
cp .env.example .env
```

Edit `.env`:

- `APP_DOMAIN=dmp.gs-empire.com`
- `ACME_EMAIL=<your-email>`

Start:

```bash
docker compose up -d --build
```

Check:

```bash
docker compose ps
docker compose logs -f caddy
docker compose logs -f app
```

App URL:

- `https://dmp.gs-empire.com`

## 5. Update after code change

```bash
git pull
docker compose up -d --build
```
