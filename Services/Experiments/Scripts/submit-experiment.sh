#!/bin/bash

# Executes a single experiment and exports data in the experiment path
# Based on instructions from hadoop-startup.md, autotest.sh, ...

# Get command line args
docker_path=
dataset_path=
dataset_name=
node_count=
class_name=
jar_path=
driver_memory=
driver_cores=
executer_number=
executer_memory=
executer_cores=
memory_overhead=
hdfs_output_directory=
local_output_directory=
optional_start=

# Add Docker Contatainers
echo "------Attempting to add containers-----"
$docker_path/up.sh

# Setup Hadoop
echo "-----Attempting to setup hadoop-----"
$docker_path/mkdir-hdfs-hadoop-home.sh
    
echo "-----Beginning experiment-----"
echo "-----Attempting to scale node count to $node-----"
$docker_path/scale.sh $node_count

# Add data set
echo "-----Attempting to add data set-----"
docker run --rm --name dataset-injector --network "$(basename "$(pwd)")_cluster-network" -v "$(pwd)/data:/mnt${dataset_path}" spark-hadoop:latest hdfs dfs -put "/mnt${dataset_path}/${dataset_name}" /user/hadoop

echo "-----Attempting to submit for trial-----"
docker exec "$(docker inspect --format '{{.Status.ContainerStatus.ContainerID}}' "$(docker service ps -q "$(basename "$(pwd)")_master" --filter desired-state=running)")" \ 
    /opt/spark/bin/spark-submit  \
    --master yarn \
    --driver-memory $driver_memory \
    --driver-cores $driver_cores \
    --num-executers $executer_number \
    --executer-cores $executer_cores \
    --executer-memory $executer_memory \
    --conf spark.executer.memoryOverhead=$memory_overhead \
    --class $class_name $jar_path $hdfs_output_directory $dataset_name $data_output_dir ${@:optional_start}
    

echo "-----Attempting to output results for experiment-----"
if [ ! -d results ] ; then
    mkdir results
fi
    fileowner="$(stat -c '%U' "results")"
if [ "${fileowner}" != "justinh225" ] ; then
    sudo chown -R justinh225 results
fi
docker run --rm --name results-extractor --network "$(basename "$(pwd)")_cluster-network" -v "$(pwd)/results:/mnt${local_output_directory}" \
    spark-hadoop:latest hdfs dfs -getmerge ${hdfs_output_directory}/${output_name} /mnt$results_output_dir/output$j.txt

echo "-----Attempting to remove containers-----"
$docker_path/down.sh
