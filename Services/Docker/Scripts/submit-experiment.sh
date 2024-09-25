!#/bin/bash

# TODO
# Spins up an instance of docker-swarm and HDFS for a specific user
# Assumes the path for HDFS has already been updated in hdfs-site.xml
# DOES NOT export output or shutdown
# Based on instructions from hadoop-startup.md, autotest.sh, ...

# Get command line args
docker_path=$1
dataset_name=$2
trials=$3
node_counts=$4

next_arg_position=$((5 + $node_counts))
num_classes=$((6 + $node_counts))
num_trees=$((7 + $node_counts))
impurity=$((8 + $node_counts))
max_depth=$((9 + $node_counts))
max_bins=$((10 + $max_bins))
output_name=$((11 + $node_counts))
percent_labeled=$((12 + $node_counts))
optional_start=((13 + $node_counts))

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
        $docker_path/submit.sh $class_name $jar_path $num_classes $num_trees $impurity $max_depth $max_bins $dataset_name $output_name $percent_labeled ${@$optional_start}
    done
done