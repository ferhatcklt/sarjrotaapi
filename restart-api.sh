#!/bin/bash
# API'yi güvenle yeniden başlatır
# Kullanım: ./restart-api.sh [--reset-db]

pkill -f "ElektrikliRota.WebApi" 2>/dev/null
sleep 1
lsof -ti :5261 | xargs kill -9 2>/dev/null
sleep 1

if [ "$1" == "--reset-db" ]; then
  rm -f ../ElektrikliRota.Infrastructure/Data/sarjrota.db
  rm -f ElektrikliRota.Infrastructure/Data/sarjrota.db
  echo "✓ Veritabanı silindi — yeniden oluşturulacak"
fi

echo "✓ Port 5261 serbest, API başlatılıyor..."
if [ -f "$HOME/.dotnet/dotnet" ]; then
  $HOME/.dotnet/dotnet run --project ElektrikliRota.WebApi
else
  dotnet run --project ElektrikliRota.WebApi
fi
