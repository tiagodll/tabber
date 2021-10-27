cd src
sudo docker stop tabber
sudo docker build -t tabberimg . --rm
sudo docker run -p 7000:7000 -t --rm \
  --env ASPNETCORE_ENVIRONMENT=Production \
  --env ASPNETCORE_URLS=http://+:7000 \
  --name tabber -d -it tabberimg
cd ..
