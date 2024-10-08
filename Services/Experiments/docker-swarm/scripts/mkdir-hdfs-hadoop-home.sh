#!/bin/bash

docker exec "$(docker inspect --format '{{.Status.ContainerStatus.ContainerID}}' "$(docker service ps -q "$(basename "$(pwd)")_master" --filter desired-state=running)")" sh -c 'hdfs dfs -mkdir -p /user/hadoop && hdfs dfs -chown hadoop:hadoop /user/hadoop'

