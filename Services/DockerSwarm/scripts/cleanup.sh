dataset_path=$1
hdfs_relative_output=$2

echo "-----Cleaning up experiment files-----"
echo "-----Attempting to delete the dataset-----"
docker run --rm --name delete_dataset --network "$(basename $(pwd) | sed 's/\./_/g')_cluster-network" \
                spark-hadoop:latest hdfs dfs -rm "$(basename ${dataset_path})"

docker run --rm --name delete_output --network "$(basename $(pwd) | sed 's/\./_/g')_cluster-network" \
        spark-hadoop:latest hdfs dfs -rm -r "${hdfs_relative_output}"

echo "-----Attempting to shutdown containers-----"
docker stack rm "$(basename $(pwd) | sed 's/\./_/g')"