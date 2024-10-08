#!/bin/bash

# Executes a single experiment and exports data in the experiment path
# Based on instructions from hadoop-startup.md, autotest.sh, ...

# Get command line args
dataset_path=$1
dataset_name=$2
node_count=$3
driver_memory=$4
driver_cores=$5
executer_number=$6
executer_cores=$7
executer_memory=$8
memory_overhead=$9

class_name=${10}
jar_path=${11}

hdfs_output_directory=${12}
results_output_directory=${13}
output_name=${14}

# Add Docker Contatainers
echo "------Attempting to add containers-----"
docker stack deploy -c docker-compose.yml "$(basename "$(pwd)")"
./scripts/scale.sh 0
docker service update --mount-add 'type=volume,source=datanode-vol-SERV{{.Service.Name}}-NODE{{.Node.ID}}-TASK{{.Task.Slot}},target=/opt/hadoop/data' "$(basename "$(pwd)")_worker"

# Setup Hadoop
echo "-----Attempting to setup hadoop-----"
./scripts/mkdir-hdfs-hadoop-home.sh


echo "-----Beginning experiment-----"
echo "-----Attempting to scale node count to $node_count-----"
./scripts/scale.sh $node_count

# Add data set
echo "-----Attempting to add data set-----"
docker run --rm --name dataset-injector --network "$(basename "$(pwd)")_cluster-network" -v "$(pwd)/data:/mnt${dataset_path}" spark-hadoop:latest hdfs dfs -put "/mnt${dataset_path}/${dataset_name}" /user/hadoop

echo "-----Waiting for Hadoop startup-----"
sleep 60

echo "-----Attempting to submit for trial-----"
docker exec "$(docker inspect --format '{{.Status.ContainerStatus.ContainerID}}' "$(docker service ps -q "$(basename "$(pwd)")_master" --filter desired-state=running)")" /opt/spark/bin/spark-submit \
    --master yarn \
    --driver-memory "$driver_memory" \
    --driver-cores $driver_cores \
    --num-executors $executer_number \
    --executor-cores $executer_cores \
    --executor-memory "$executer_memory" \
    --conf spark.executor.memoryOverhead=$memory_overhead \
    --class "$class_name" "/opt/jars/$jar_path" $dataset_name $hdfs_output_directory $output_name ${@:15}
    

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
./scripts/down.sh
