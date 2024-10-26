#!/bin/bash

current_user=$1

advertise_ip=$2
advertise_port=$3

docker_images_dir=$4
results_dir=$5
data_directory=$6

# Add user to docker group if not already
echo "-----Checking if user is in the docker group-----"
if ! groups $current_user | grep -qc 'docker'
then 
    echo " ${current_user} is not in the docker group." >&2
    exit 1
fi 


# Check Docker-Swarm is active
echo "-----Checking if docker swarm is active-----"
if ! docker info | grep -qc 'Swarm: active'
then
    echo "-----Initializing Docker Swarm-----"
    response=-1
    if [[ $advertise_ip == "-1" && $advertise_port == "-1" ]] ; then
        echo "-----Initializing Docker Swarm with address $advertise_ip:$advertise_port-----"
        docker swarm init
        response=$?
    else
        echo "-----Initializing Docker Swarm with default address-----"
        docker swarm init --advertise-addr "${advertise_ip}:$advertise_port"
        response=$?
    fi
    if [[ $response != 0 ]]
    then
	echo " Failed to initialize swarm." >&2
        exit 1
    fi 
fi


# Check we have the manager node added
echo "-----Checking if we have joined the swarm as a manager-----"
if docker info | grep -qc 'Managers: 0'
then
    echo "-----Adding node as a manager-----"
    join_manager=$(docker swarm join-token manager | grep -Eo 'docker swarm join --token [A-Za-z0-9.:-]+ [0-9.:]+')
    eval $join_manager
    
    if [[ $? != 0 ]]
    then
	echo " Failed to add node as manager." >&2
		exit 1
    fi
fi


# Enable access to docker-images directory
echo "-----Setting docker-images permissions-----"
if [ ! -d $docker_images_dir] 
then
    echo "Error: Docker-images directory does not exist." >&2
    exit 1
else
    setfacl -Rm u:$current_user:rwx $docker_images_dir
fi


# Enable access to data directory
echo "-----Setting data permissions-----"
if [ ! -d $data_directory]
then
    echo "Error: Data directory does not exist." >&2
    exit 1
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