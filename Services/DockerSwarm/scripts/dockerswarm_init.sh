#!/bin/bash

$current_user=$1

$docker_images_dir=$2
$results_dir=$3
$data_directory=$4

# Enable access to docker-images directory
if [ ! -d $docker_images_dir] 
then
    exit 1
else
    setfacl -Rm u:$current_user:rwx $docker_images_dir
fi

# Enable access to data directory
if [ ! -d $data_directory]
then
    exit 2
else
    setfacl -Rm u:$current_user:rwx $data_dir
fi

# Make sure a results directory exists and/or enable access
if [ ! -d $result_dir]
then
    mkdir $result_dir
fi
setfacl -Rm u:$current_user:rwx $result_dir
