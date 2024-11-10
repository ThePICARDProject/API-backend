dataset_path=$1
hdfs_relative_output=$2
timeout=$3

echo "-----Cleaning up experiment files-----"
docker run --rm --name delete_dataset --network "$(basename $(pwd) | sed 's/\./_/g')_cluster-network" \
	spark-hadoop:latest hdfs dfs -rm "$(basename ${dataset_path})"

docker run --rm --name delete_output --network "$(basename $(pwd) | sed 's/\./_/g')_cluster-network" \
	spark-hadoop:latest hdfs dfs -rm -r "${hdfs_relative_output}"

echo "-----Attempting to shutdown containers-----"
docker stack rm "$(basename $(pwd) | sed 's/\./_/g')"

echo "-----Waiting for network to close-----"
for i in $(seq 1 $timeout) 
do
	echo "-----Waiting for the network to close... Attempt $i of $timeout-----"
	if [[ $(docker network ls | grep -c "$(basename $(pwd) | sed 's/\./_/g')_cluster-network") == 0 ]]
	then
		break
	elif [[ $i == $timeout ]]
	then
		echo "Failed to close cluster_network. Timed Out." 1>&2
		exit 1
	fi
	sleep 1	
done
