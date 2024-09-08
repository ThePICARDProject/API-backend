#!/bin/bash

docker run --rm --name results-extractor --network \"$(basename \"$(pwd)\")_cluster-network\" -v \"$(pwd)/results:/mnt/results\" spark-hadoop:latest hdfs dfs -getmerge /data/results/palfa/output