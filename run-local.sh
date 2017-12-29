#!/bin/bash
mono packages/FAKE/tools/FAKE.exe build.fsx
echo 'Building docker image'
sudo docker build -t bittacklr-image:dev .
echo 'Running docker container based on image'
sudo docker run --name bittacklrdev -d -p 80:80 bittacklr-image:dev
echo 'Press enter to exit'
read -s -n 1 key
echo 'Stopping docker container'
sudo docker stop bittacklrdev
echo 'Removing docker container'
sudo docker rm bittacklrdev
echo 'Removing docker image'
sudo docker rmi bittacklr-image:dev