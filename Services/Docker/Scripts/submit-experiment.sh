!#/bin/bash

# TODO
# Spins up an instance of docker-swarm and HDFS for a specific user
# Assumes the path for HDFS has already been updated in hdfs-site.xml
# DOES NOT export output or shutdown
# Based on instructions from hadoop-startup.md, autotest.sh, ...

# Get command line args
docker_path=$1
dataset=$2
trials=$3

# Start Docker containers
docker swarm init
docker swarm join-token manager

# Setup hadoop
$docker_path/mkdir-hdfs-hadoop-home.sh

# Add Dataset
$docker_path/data-in.sh $dataset

# Run for each node count
end_index=${{2 + $2}}
for node_count_index in $(seq 3 $end_index); do
    
    # Scale node count
    $docker_path/scale.sh $node_count_index

    # Iterate trials
    for j in $(seq 1 $trials); do
        # Submit
    done
done