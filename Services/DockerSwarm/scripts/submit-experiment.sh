#!/bin/bash

# Executes a single experiment and exports data in the experiment path
# Based on instructions from hadoop-startup.md, autotest.sh, ...

# Get command line arg:
current_user=$1
dataset_path=$2
dataset_name=$3
node_count=$4
driver_memory=$5
driver_cores=$6
executer_number=$7
executer_cores=$8
executer_memory=$9
memory_overhead=${10}

class_name=${11}
jar_path=${12}

hdfs_url=${13}
results_output_directory=${14}
hdfs_relative_output=${15}

echo "-----Rebuilding docker images-----"
docker compose build

echo "-----Attempting to add nodes-----"
docker stack deploy -c docker-compose.yml "$(basename $(pwd) | sed 's/\./_/g')"
docker service scale "$(basename $(pwd) | sed 's/\./_/g')_worker"=0
docker service update --mount-add 'type=volume,source=datanode-vol-SERV{{.Service.Name}}-NODE{{.Node.ID}}-TASK{{.Task.Slot}},target=/opt/hadoop/data' "$(basename $(pwd) | sed 's/\./_/g')"

echo "-----Attempting to setup hadoop-----"
docker exec "$(docker inspect --format '{{.Status.ContainerStatus.ContainerID}}' "$(docker service ps -q "$(basename $(pwd) | sed 's/\./_/g')_master" --filter desired-state=running)")" \
       	sh -c 'hdfs dfs -mkdir -p /user/hadoop && hdfs dfs -chown hadoop:hadoop /user/hadoop'


echo "-----Attempting to scale node count to ${node_count}-----"
docker service scale "$(basename $(pwd) | sed 's/\./_/g')_worker"="${node_count}"


echo "-----Giving time for Hadoop to come online-----"
sleep 15
# Force hadoop out of safemode REMOVE LATER
docker run --rm --name delete_dataset --network "$(basename $(pwd) | sed 's/\./_/g')_cluster-network" \
	spark-hadoop:latest hdfs dfsadmin -safemode leave

# Add data set
echo "-----Attempting to add data set-----"
docker run --rm --name dataset-injector --network "$(basename $(pwd) | sed 's/\./_/g')_cluster-network" -v "${dataset_path}:/mnt/data" spark-hadoop:latest hdfs dfs -put "/mnt/data/${dataset_name}" /user/hadoop

echo "-----Beginning experiment-----"
docker exec "$(docker inspect --format '{{.Status.ContainerStatus.ContainerID}}' "$(docker service ps -q "$(basename $(pwd) | sed 's/\./_/g')_master" --filter desired-state=running)")" /opt/spark/bin/spark-submit \
    --master yarn \
    --driver-memory "$driver_memory" \
    --driver-cores $driver_cores \
    --num-executors $executer_number \
    --executor-cores $executer_cores \
    --executor-memory "$executer_memory" \
    --conf spark.executor.memoryOverhead=$memory_overhead \
    --class "${class_name}" "/opt/jars/${jar_path}" $dataset_name $hdfs_url $hdfs_relative_output ${@:16}


echo "-----Attempting to output results for experiment-----"
if [ ! -d $results_output_directory ] ; then
	mkdir -p $results_output_directory
fi

fileowner="$(stat -c '%U' "${results_output_directory}")"
if [ "${fileowner}" != "${current_user}" ] ; then 
	sudo chown -R "${current_user}" "${results_output_directory}"
fi
docker run --rm --name results-extractor --network "$(basename $(pwd) | sed 's/\./_/g')_cluster-network" -v "${results_output_directory}:/mnt/results" \
	spark-hadoop:latest hdfs dfs -getmerge ${hdfs_url}/${hdfs_relative_output} "/mnt/results/$(basename ${hdfs_relative_output})"


echo "-----Cleaning up experiment files-----"
echo "-----Attempting to delete the dataset-----"
docker run --rm --name delete_dataset --network "$(basename $(pwd) | sed 's/\./_/g')_cluster-network" \
	    spark-hadoop:latest hdfs dfs -rm $dataset_name


echo "-----Attempting to shutdown containers-----"
docker stack rm "$(basename $(pwd) | sed 's/\./_/g')"