#!/bin/bash

$current_user=$1
$docker_images_dir=$2
$results_dir=$3
$data_directory=$4

# Add user to docker group if not already
echo "-----Checking if user is in the docker group-----"
if ! groups $current_user | grep -qc 'docker' 
then
    echo "-----Adding user ${current_user} to docker group-----"
    sudo usermod -aG docker $current_user
fi

# Check Docker-Swarm is active
echo "-----Checking if docker swarm is active-----"
if ! docker info | grep -qc 'Swarm: active'
then
    echo "-----Initializing Docker Swarm-----"
    docker swarm init --advertise-addr "0.0.0.0:0000" #change this accordingly
fi

# Check we have the manager node added
echo "-----Checking if we have joined the swarm as a manager-----"
if docker info | grep -qc 'Managers: 0'
then
    echo "-----Adding node as a manager-----"
    join_manager=$(docker swarm join-token manager | grep -Eo 'docker swarm join --token [A-Za-z0-9.:-]+ [0-9.:]+')
    echo $(eval $join_manager)
if

# Enable access to docker-images directory
echo "-----Setting docker-images permissions-----"
if [ ! -d $docker_images_dir] 
then
    exit 1
else
    setfacl -Rm u:$current_user:rwx $docker_images_dir
fi

# Enable access to data directory
echo "-----Setting data permissions-----"
if [ ! -d $data_directory]
then
    exit 2
else
    setfacl -Rm u:$current_user:rwx $data_dir
fi

# Make sure a results directory exists and/or enable access
echo "-----Setting results permissions-----"
if [ ! -d $result_dir]
then
    mkdir $result_dir
fi
setfacl -Rm u:$current_user:rwx $result_dir
