#!/bin/bash
# Adds docker containers and sets up HDFS within the containers
# DOES NOT export results
# Based on instructions from hadoop-startup.md, autotest.sh, ...

# Get command line args
docker_path=$1
dataset_path=$2
dataset_name=$3
trials=$4
node_counts=$5
position=$((6 + $node_counts))
driver_memory=${!position}
position=$(($position + 1))
#driver_cores=$((7 + $node_counts))
#echo $driver_cores
#executer_number=$((8 + $node_counts))
#echo $executer_number
executer_memory=${!position}
position=$(($position + 1))
executer_cores=${!position}
position=$(($position + 1))
memory_overhead=${!position}
position=$((position + 1))
num_classes=${!position}
position=$(($position + 1))
num_trees=${!position}
position=$(($position + 1))
impurity=${!position}
position=$(($position + 1))
max_depth=${!position}
position=$(($position + 1))
max_bins=${!position}
position=$(($position + 1))
percent_labeled=${!position}
position=$(($position + 1))
hdfs_output_directory=${!position}
position=$(($position + 1))
local_output_directory=${!position}
position=$(($position + 1))
optional_start=$position

# Add Docker Contatainers
echo "------Attempting to add containers-----"
$docker_path/up.sh

# Setup Hadoop
echo "-----Attempting to setup hadoop-----"
$docker_path/mkdir-hdfs-hadoop-home.sh

# Run for each node count
echo "-----Beginning experiment-----"
end_index=$((2 + $node_counts))
for node_count_index in $(seq 3 $end_index); do
    
    # Scale node count
    echo "-----Attempting to scale node count to ${!node_count_index}-----"
    $docker_path/scale.sh $node_count_index

    # Add data set
    echo "-----Attempting to add data set-----"
    docker run --rm --name dataset-injector --network "$(basename "$(pwd)")_cluster-network" -v "${dataset_path}" spark-hadoop:latest hdfs dfs -put "/mnt${dataset_path}/${dataset_name}" /user/hadoop

    # Iterate trials
    for j in $(seq 1 $trials); do
        # Submit
	echo "-----Attempting to submit for trial $j-----"
        docker exec "$(docker inspect --format '{{.Status.ContainerStatus.ContainerID}}' "$(docker service ps -q "$(basename "$(pwd)")_master" --filter desired-state=running)")" \ 
            /opt/spark/bin/spark-submit  \
            --master yarn \
            --driver-memory $driver_memory \
            #--driver-cores $driver_cores \
            --num-executers $executer_number \
	    --executer-cores $executer_cores \
            --executer-memory $executer_memory \
            --conf spark.executer.memoryOverhead=$memory_overhead \
            --class $class_name $jar_path $num_classes $num_trees $impurity $max_depth $max_bins $dataset_name $data_output_dir $percent_labeled ${@:optional_start}
    

        # Output a results file
        # Change file owner accordingly
	echo "-----Attempting to output results for trial $j-----"
        if [ ! -d results ] ; then
	        mkdir results
        fi
        fileowner="$(stat -c '%U' "results")"
        if [ "${fileowner}" != "justinh225" ] ; then
	        sudo chown -R justinh225 results
        fi
	
	echo "-----Attempting to remove data set-----"
        docker run --rm --name results-extractor --network "$(basename "$(pwd)")_cluster-network" -v "$(pwd)/results:/mnt/results" \
        spark-hadoop:latest hdfs dfs -getmerge $data_output_dir $results_output_dir/output$j.txt
    done

    # Remove dataset from HDFS
    # Do this in order to avoid corrupting before scale
    # Added since rebalance.sh is not working
    # docker run --rm --name dataset-remove --network "$(basename "$(pwd)")_cluster-network" \
    # spark-hadoop:latest hdfs dfs -rm /user/hadoop
done

# REMOVE THIS, will need to get output before removing stack
# Remove Nodes
echo "-----Attempting to remove containers-----"
$docker_path/down.sh
