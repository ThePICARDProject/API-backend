# The PICARD Project: API and Backend Implementation

## Table of Contents
1. Overview
2. Setup
3. API Documentation
4. Help/Resources

## Overview

## Setup

1. Install .NET using the [Microsoft Learn Documentation](https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-install?tabs=dotnet9&pivots=os-linux-ubuntu-2410)
2. Install Docker by following the instructions outlined in the [Docker Documentation](https://docs.docker.com/engine/install/ubuntu/)
3. Install and setup the database schema by following the [MySql instructions](https://github.com/ThePICARDProject/database-schema-backend/blob/main/README.md)
4. Clone the repository in the desired directory `git clone https://github.com/ThePICARDProject/API-backend`
5. Follow steps 1-4 in the [DockerSwarm C# Quick Start](/Services/DockerSwarm/dockerswarm.md) to set up the DockerSwarm service.
6. Navigate to `./bin/Debug/net8.0` and execute `dotnet API-backend.dll`
7. The backend service should run and begin initializing Docker Swarm.

## API Documentation

### API-backend
### Version: 1.0

### /api/algorithms/algorithms

#### GET
##### Summary:

Gets all of a users algorithms stored in the database

##### Description:

Gets all of a users algorithms stored in the database

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

### /api/algorithms/algorithmParameters

#### GET
##### Summary:

Gets all algorithm parameter definitions for a users algorithm

##### Description:

Gets all algorithm parameter definitions for a users algorithm

##### Parameters

| Name | Located in | Description | Required | Schema |
| ---- | ---------- | ----------- | -------- | ---- |
| algorithmId | query |  | No | integer |

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

### /api/algorithms/upload

#### POST
##### Summary:

Uploads a users algorithm to the database

##### Description:

Uploads a users algorithm to the database

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

### /Authentication/login

#### GET
##### Summary:

Redirects user to the Google OAuth page for authentication

##### Description:

Redirects user to the Google OAuth page for authentication

##### Parameters

| Name | Located in | Description | Required | Schema |
| ---- | ---------- | ----------- | -------- | ---- |
| returnUrl | query |  | No | string |

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

### /Authentication/logout

#### GET
##### Summary:

Logs out an authenticated user

##### Description:

Logs out an authenticated user

##### Parameters

| Name | Located in | Description | Required | Schema |
| ---- | ---------- | ----------- | -------- | ---- |
| returnUrl | query |  | No | string |

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

### /api/dataset

#### GET
##### Summary:

Gets all of a users dataset information

##### Description:

Gets all of a users dataset information

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

### /api/dataset/{id}

#### GET
##### Summary:

Gets a dataset from a dataset id

##### Description:

Gets a dataset from a dataset id

##### Parameters

| Name | Located in | Description | Required | Schema |
| ---- | ---------- | ----------- | -------- | ---- |
| id | path |  | Yes | integer |

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

### /api/dataset/download/{id}

#### GET
##### Summary:

Downloads a user's dataset by Id

##### Description:

Downloads a user's dataset by Id

##### Parameters

| Name | Located in | Description | Required | Schema |
| ---- | ---------- | ----------- | -------- | ---- |
| id | path |  | Yes | integer |

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

### /api/dataset/upload

#### POST
##### Summary:

Uploads a user's dataset and metadata

##### Description:

Uploads a user's dataset and metadata

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

### /api/experiment/submit

#### POST
##### Summary:

Submits an experiment to the cluster

##### Description:

Submits an experiment to the cluster

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

### /api/experiment/status/{experimentId}

#### GET
##### Summary:

Gets the status of a submitted experiment

##### Description:

Gets the status of a submitted experiment

##### Parameters

| Name | Located in | Description | Required | Schema |
| ---- | ---------- | ----------- | -------- | ---- |
| experimentId | path |  | Yes | string (uuid) |

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

### /api/result/getProcessedResults/{aggregateDataId}

#### GET
##### Summary:

Downloads a user's processed results

##### Description:

Downloads a user's processed results

##### Parameters

| Name | Located in | Description | Required | Schema |
| ---- | ---------- | ----------- | -------- | ---- |
| aggregateDataId | path |  | Yes | integer |

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

### /api/result/aggregateData

#### POST
##### Summary:

Aggregates individual results file and concatentates them into a single file based on given parameter values

##### Description:

Aggregates individual experiment results files and concatentates them into a single file based on given parameter values. Cluster parameters values will be interpreted as an integer by default, if there is quotes as part of the provided value it will be interpreted as a string. Queries database for experiment results where their selected parameter values are equal the intersection of the provided cluster parameters and algorithm parameters.

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

### /api/result/createCsv

#### POST
##### Summary:

Creates a .csv file for an aggregated data result based on its id

##### Description:

Creates a .csv file for an aggregated data result based on its id. The metrics identifiers must match the raw data files exactly, and the identifier and the value must be seperated by a single equals sign.

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

### /api/result/DockerSwarmParams

#### GET
##### Summary:

Gets all cluster parameters in the database

##### Description:

Gets all cluster parameters in the database

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

### /User/userinfo

#### GET
##### Summary:

Gets user info

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

### /api/Visualization

#### POST
##### Summary:

Not Currently Working

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |


## Help/Resources
