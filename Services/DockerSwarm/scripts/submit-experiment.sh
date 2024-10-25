#!/bin/bash

# Executes a single experiment and exports data in the experiment path
# Based on instructions from hadoop-startup.md, autotest.sh, ...

current_user=$1
log_path=$2
dataset_path=$3
dataset_name=$4
node_count=$5
driver_memory=$6
driver_cores=$7
executer_number=$8
executer_cores=$9
executer_memory=${10}
memory_overhead=${11}
class_name=${12}
jar_path=${13}
hdfs_url=${14}
results_output_directory=${15}
hdfs_relative_output=${16}


echo "-----Setting up directories-----"
if [ ! -d $results_output_directory ] ; then
	mkdir -p $results_output_directory
fi
fileowner="$(stat -c '%U' "${results_output_directory}")"
if [ "${fileowner}" != "${current_user}" ] ; then
	sudo chown -R "${current_user}" "${results_output_directory}"
fi
touch $log_path

sleep 15

echo "-----Attempting to run experiment with args:-----" | tee -a $log_path
echo "current_user: ${current_user}" | tee -a $log_path
echo "log_path: ${log_path}" | tee -a $log_path
echo "dataset_path: ${dataset_path}" | tee -a $log_path 
echo "dataset_name: ${dataset_name}" | tee -a $log_path
echo "node_count: ${node_count}" | tee -a $log_path
echo "driver_memory: ${driver_memory}" | tee -a $log_path
echo "driver_cores: ${driver_cores}" | tee -a $log_path
echo "executer_number: ${executer_number}" | tee -a $log_path
echo "executer_cores: ${executer_cores}" | tee -a $log_path
echo "executer_memory: ${executer_memory}" | tee -a $log_path
echo "memory_overhead: ${memory_overhead}" | tee -a $log_path
echo "class_name: ${class_name}" | tee -a $log_path
echo "jar_path: ${jar_path}" | tee -a $log_path
echo "hdfs_url: ${hdfs_url}" | tee -a $log_path
echo "results_output_directory: ${results_output_directory}" | tee -a $log_path
echo "hdfs_relative_output: ${hdfs_relative_output}" | tee -a $log_path
echo "algorithm_params: ${@:17}" | tee -a $log_path


echo "-----Rebuilding docker images-----" | tee -a $log_path
docker compose build | tee -a $log_path

echo "-----Attempting to add nodes-----"
docker stack deploy -c docker-compose.yml "$(basename $(pwd) | sed 's/\./_/g')" | tee -a $log_path
docker service scale "$(basename $(pwd) | sed 's/\./_/g')_worker"=0 | tee -a $log_path
docker service update --mount-add 'type=volume,source=datanode-vol-SERV{{.Service.Name}}-NODE{{.Node.ID}}-TASK{{.Task.Slot}},target=/opt/hadoop/data' "$(basename $(pwd) | sed 's/\./_/g')_worker" | tee -a $log_path

sleep 30

echo "-----Attempting to setup hadoop-----" | tee -a $log_path
docker exec "$(docker inspect --format '{{.Status.ContainerStatus.ContainerID}}' "$(docker service ps -q "$(basename $(pwd) | sed 's/\./_/g')_master" --filter desired-state=running)")" \
       	sh -c 'hdfs dfs -mkdir -p /user/hadoop && hdfs dfs -chown hadoop:hadoop /user/hadoop' | tee -a $log_path


echo "-----Attempting to scale node count to ${node_count}-----" | tee -a $log_path
docker service scale "$(basename $(pwd) | sed 's/\./_/g')_worker"="${node_count}" | tee -a $log_path


echo "-----Giving time for Hadoop to come online-----" | tee -a $log_path
sleep 15
# Force hadoop out of safemode REMOVE LATER
docker run --rm --name delete_dataset --network "$(basename $(pwd) | sed 's/\./_/g')_cluster-network" \
	spark-hadoop:latest hdfs dfsadmin -safemode leave | tee -a $log_path

# Add data set
echo "-----Attempting to add data set-----" | tee -a $log_path
docker run --rm --name dataset-injector --network "$(basename $(pwd) | sed 's/\./_/g')_cluster-network" -v "${dataset_path}:/mnt/data" spark-hadoop:latest hdfs dfs -put "/mnt/data/${dataset_name}" /user/hadoop | tee -a $log_path

echo "-----Beginning experiment-----" | tee -a $log_path
docker exec "$(docker inspect --format '{{.Status.ContainerStatus.ContainerID}}' "$(docker service ps -q "$(basename $(pwd) | sed 's/\./_/g')_master" --filter desired-state=running)")" /opt/spark/bin/spark-submit \
    --master yarn \
    --driver-memory "$driver_memory" \
    --driver-cores $driver_cores \
    --num-executors $executer_number \
    --executor-cores $executer_cores \
    --executor-memory "$executer_memory" \
    --conf spark.executor.memoryOverhead=$memory_overhead \
    --class "${class_name}" "/opt/jars/${jar_path}" $dataset_name $hdfs_url $hdfs_relative_output ${@:17} | tee -a $log_path


echo "-----Attempting to output results for experiment-----" | tee -a $log_path
docker run --rm --name results-extractor --network "$(basename $(pwd) | sed 's/\./_/g')_cluster-network" -v "${results_output_directory}:/mnt/results" \
	spark-hadoop:latest hdfs dfs -getmerge ${hdfs_url}/${hdfs_relative_output} "/mnt/results/$(basename ${hdfs_relative_output})" | tee -a $log_path
 

echo "-----Cleaning up experiment files-----" | tee -a $log_path
echo "-----Attempting to delete the dataset-----" | tee -a $log_path
docker run --rm --name delete_dataset --network "$(basename $(pwd) | sed 's/\./_/g')_cluster-network" \
	    spark-hadoop:latest hdfs dfs -rm $dataset_name | tee -a $log_path


echo "-----Attempting to shutdown containers-----" | tee -a $log_path
docker stack rm "$(basename $(pwd) | sed 's/\./_/g')" | tee -a $log_path

