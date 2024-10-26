dataset_name=$1

echo "-----Cleaning up experiment files-----"
echo "-----Attempting to delete the dataset-----"
docker run --rm --name delete_dataset --network "$(basename $(pwd) | sed 's/\./_/g')_cluster-network" \
	    spark-hadoop:latest hdfs dfs -rm $dataset_name


echo "-----Attempting to shutdown containers-----"
docker stack rm "$(basename $(pwd) | sed 's/\./_/g')"