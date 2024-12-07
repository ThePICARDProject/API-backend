#!/bin/bash

# Executes a single experiment and exports data in the experiment path
# Based on instructions from hadoop-startup.md, autotest.sh, ...

current_user=$1
log_path=$2
dataset_path=$3
node_count=$4
driver_memory=$5
driver_cores=$6
executor_number=$7
executor_cores=$8
executor_memory=${9}
memory_overhead=${10}
class_name=${11}
jar_path=${12}
hdfs_url=${13}
results_output_directory=${14}
hdfs_relative_output=${15}

# Control Variables
timeout=30 # Sets maximum attempts waiting for a response. Should be at least 15.

# Function for checking a scripts error code and cleaning up
check_error() {
	if [[ $? != 0 ]]
	then
		./scripts/cleanup.sh "${dataset_path}" "/${hdfs_relative_output}" $timeout &>> $log_path
		exit 1
	fi
}

echo "-----Submitting new Experiment-----"
echo "-----Creating user specific directories-----"
if [ ! -d $results_output_directory ] ; then
	mkdir -p $results_output_directory
fi

if [ ! -s $dataset_path ] ; then
	echo "Dataset path does not exist"
	exit 1
fi


touch $log_path


echo "-----Setting permissions-----"
setfacl -m u:hadoop:rwx ${results_output_directory} || { echo "Failed to set permissions for hadoop on the results output" 1>&2 ; exit 1; }
setfacl -m u:${current_user}:rwx ${results_output_directory} || { echo "Failed to set permissions for user on the results output" 1>&2 ; exit 1; }
				    
setfacl -m u:hadoop:rwx ${dataset_path} || { echo "Failed to set permissions for hadoop on the results output" 1>&2 ; exit 1; }
setfacl -m u:${current_user}:rwx ${dataset_path} || { echo "Failed to set permissions for use dataset" 1>&2 ; exit 1; }

					    
echo "-----Attempting to run experiment with args:" | tee -a $log_path
echo "current_user: ${current_user}" | tee -a $log_path
echo "log_path: ${log_path}" | tee -a $log_path
echo "dataset_path: ${dataset_path}" | tee -a $log_path
echo "dataset_name: $(basename ${dataset_path})" | tee -a $log_path
echo "node_count: ${node_count}" | tee -a $log_path
echo "driver_memory: ${driver_memory}" | tee -a $log_path
echo "driver_cores: ${driver_cores}" | tee -a $log_path
echo "executor_number: ${executor_number}" | tee -a $log_path
echo "executor_cores: ${executor_cores}" | tee -a $log_path
echo "executor_memory: ${executor_memory}" | tee -a $log_path
echo "memory_overhead: ${memory_overhead}" | tee -a $log_path
echo "class_name: ${class_name}" | tee -a $log_path
echo "jar_path: ${jar_path}" | tee -a $log_path
echo "hdfs_url: ${hdfs_url}" | tee -a $log_path
echo "results_output_directory: ${results_output_directory}" | tee -a $log_path
echo "hdfs_relative_output: ${hdfs_relative_output}" | tee -a $log_path
echo "algorithm_params: ${@:17}" | tee -a $log_path
echo "-----" | tee -a $log_path


echo "-----Rebuilding docker images-----" | tee -a $log_path
docker compose build &>> $log_path
check_error


echo "-----Attempting to add nodes-----" | tee -a $log_path
docker stack deploy -c docker-compose.yml "$(basename $(pwd) | sed 's/\./_/g')" &>> $log_path
check_error
docker service scale "$(basename $(pwd) | sed 's/\./_/g')_worker"=0 &>> $log_path
check_error
docker service update --mount-add 'type=volume,source=datanode-vol-SERV{{.Service.Name}}-NODE{{.Node.ID}}-TASK{{.Task.Slot}},target=/opt/hadoop/data' "$(basename $(pwd) | sed 's/\./_/g')_worker" &>> $log_path
check_error

echo "-----Setting up HDFS-----" | tee -a $log_path
for i in $(seq 1 $timeout)
do
	echo "-----Trying to connect to Hadoop... Attempt $i of $timeout-----"
	if [[ $(docker exec "$(docker inspect --format '{{.Status.ContainerStatus.ContainerID}}' "$(docker service ps -q "$(basename $(pwd) | sed 's/\./_/g')_master" --filter desired-state=running)")" sh -c 'hdfs dfs -mkdir -p /user/hadoop && hdfs dfs -chown hadoop:hadoop /user/hadoop' 2>&1 | grep -c "Connection refused;") == 0 ]]
	then
		echo "Connection successful"
		break
	elif [[ $i == $timeout ]]
	then
		echo "Failed to connect to hadoop. Timed Out." 1>&2
		./scripts/cleanup.sh "${dataset_path}" "/${hdfs_relative_output}" $timeout &>> $log_path
		exit 1
	fi
	sleep 1
done


echo "-----Verifying HDFS status----" | tee -a $log_path
if [[ $(docker run --rm --name poll_safe_mode --network "$(basename $(pwd) | sed 's/\./_/g')_cluster-network" spark-hadoop:latest hdfs fsck / | grep -c 'HEALTHY') == 0 ]]
then
	echo " HDFS is corrupt." | tee -a $log_path
	
	# Find a better way to handle this
	docker run --rm --name poll_safe_mode --network "$(basename $(pwd) | sed 's/\./_/g')_cluster-network" spark-hadoop:latest hdfs fsck /
	docker run --rm --name poll_safe_mode --network "$(basename $(pwd) | sed 's/\./_/g')_cluster-network" spark-hadoop:latest hdfs dfs -rm -r /data
	
	./scripts/cleanup.sh $dataset_path "/${hdfs_relative_output}" $timeout &>> $log_path
	exit 1
fi


echo "-----Attempting to scale node count to ${node_count}-----" | tee -a $log_path
docker service scale "$(basename $(pwd) | sed 's/\./_/g')_worker"="${node_count}" &>> $log_path
check_error


for i in  $(seq 1 $timeout)
do
	echo "-----Waiting for datanodes... Attempt $i of $timeout-----"
	if [[ $(docker run --rm --name check_status --network "$(basename $(pwd) | sed 's/\./_/g')_cluster-network" spark-hadoop:latest hdfs dfsadmin -report | grep -c "Live datanodes (${node_count})") != 0 ]]
	then
		break
	elif [[ $i == $timeout ]]
	then
		echo "Failed to connect to hadoop. Timed Out." 1>&2
		./scripts/cleanup.sh "${dataset_path}" "/${hdfs_relative_output}" $timeout &>> $log_path
		exit 1
	fi
	sleep 1
done


echo "-----Attempting to add data set-----" | tee -a $log_path
docker run --rm --name dataset-injector --network "$(basename $(pwd) | sed 's/\./_/g')_cluster-network" -v "${dataset_path}:/mnt/data/$(basename ${dataset_path})" spark-hadoop:latest hdfs dfs -put "/mnt/data/$(basename ${dataset_path})" "/user/hadoop" &>> $log_path
check_error


echo "-----Beginning experiment-----" | tee -a $log_path
docker exec "$(docker inspect --format '{{.Status.ContainerStatus.ContainerID}}' "$(docker service ps -q "$(basename $(pwd) | sed 's/\./_/g')_master" --filter desired-state=running)")" /opt/spark/bin/spark-submit \
	--master yarn \
	--driver-memory "$driver_memory" \
	--driver-cores $driver_cores \
	--num-executors $executor_number \
	--executor-cores $executor_cores \
	--executor-memory "$executor_memory" \
	--conf spark.executor.memoryOverhead=$memory_overhead \
	--class "${class_name}" "/opt/jars/${jar_path}" "$(basename ${dataset_path})" $hdfs_url $hdfs_relative_output ${@:16} &>> $log_path
check_error


echo "-----Attempting to output results for experiment-----" | tee -a $log_path
docker run --rm --name results-extractor --network "$(basename $(pwd) | sed 's/\./_/g')_cluster-network" -v "${results_output_directory}:/mnt/results" spark-hadoop:latest hdfs dfs -getmerge "${hdfs_url}/${hdfs_relative_output}" "/mnt/results/$(basename ${hdfs_relative_output})" &>> $log_path
check_error

./scripts/cleanup.sh "${dataset_path}" "/${hdfs_relative_output}" $timeout &>> $log_path

echo "-----Experiment Completed-----" | tee -a $log_path
