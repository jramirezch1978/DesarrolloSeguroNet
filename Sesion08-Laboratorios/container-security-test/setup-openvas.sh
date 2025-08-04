#!/bin/bash
echo "🔧 Configurando OpenVAS en la VM..."

# Actualizar sistema
sudo apt update
sudo apt install -y docker.io docker-compose
sudo systemctl start docker
sudo systemctl enable docker
sudo usermod -aG docker azureuser

# Desplegar OpenVAS usando Docker
docker run -d \
  --name openvas \
  -p 443:443 \
  -e PASSWORD=admin123 \
  --volume openvas-data:/data \
  mikesplain/openvas

# Verificar que OpenVAS está ejecutándose
echo "🔍 Verificando estado de OpenVAS..."
docker ps
docker logs openvas

echo "✅ OpenVAS configurado exitosamente"
echo "🌐 URL: https://20.57.71.59"
echo "👤 Username: admin"
echo "🔑 Password: admin123"
