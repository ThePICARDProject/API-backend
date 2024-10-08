#!/bin/bash

docker service scale "$(basename "$(pwd)")_worker"="$1"

