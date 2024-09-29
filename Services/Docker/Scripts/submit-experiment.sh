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

driver_memory=$((5 + $node_counts))
driver_cores=$((6 + $node_counts))
executer_number=$((7 + $node_counts))
executer_memory=$((8 + $node_counts))
executer_cores=$((9 + $node_counts))
memory_overhead=$((10 + $node_counts))
num_classes=$((11 + $node_counts))
num_trees=$((12 + $node_counts))
impurity=$((13 + $node_counts))
max_depth=$((14 + $node_counts))
max_bins=$((15 + $max_bins))
output_name=$((16 + $node_counts))
percent_labeled=$((17 + $node_counts))
optional_start=((18 + $node_counts))

# NOT SURE: Start Docker containers
docker swarm join
docker compose build
./up.sh

# Setup hadoop
./$docker_path/mkdir-hdfs-hadoop-home.sh

# Add Dataset
./$docker_path/data-in.sh $dataset

# Run for each node count
end_index=${{2 + $2}}
for node_count_index in $(seq 3 $end_index); do
    
    # Scale node count
    ./$docker_path/scale.sh $node_count_index

    # Iterate trials
    for j in $(seq 1 $trials); do
        # Submit
        docker exec "$(docker inspect --format '{{.Status.ContainerStatus.ContainerID}}' "$(docker service ps -q "$(basename "$(pwd)")_master" --filter desired-state=running)")" 
            /opt/spark/bin/spark-submit  \
            --master yarn \
            --driver-memory $driver_memory \
            --driver-cores $driver_cores \
            --num-executers $executer_number \
            --executer-memory $executer_memory \
            --executer-cores $executer_cores \
            --conf spark.executer.memoryOverhead=$memory_overhead \
            --class $class_name $jar_path $num_classes $num_trees $impurity $max_depth $max_bins $dataset_name $output_name $percent_labeled ${@$optional_start}
    done
done
