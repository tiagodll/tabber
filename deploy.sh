cd src
sudo docker stop tabber
sudo docker build -t tabber . --rm
sudo docker run -p 7000:7000 -t \
  --env ASPNETCORE_ENVIRONMENT=Production \
  --env ASPNETCORE_URLS=http://+:7000 \
  --name tabber -d --restart unless-stopped -it tabber
cd ..
