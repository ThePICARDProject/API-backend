# The PICARD Project: API and Backend Implementation

## Table of Contents
1. Overview
2. Quick Start
3. API Documentation
4. Current State and Future Work
5. Help/Resources

## Overview

## Quick Start

1. Install .NET using the [Microsoft Learn Documentation](https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-install?tabs=dotnet9&pivots=os-linux-ubuntu-2410)
2. Install Docker by following the instructions outlined in the [Docker Documentation](https://docs.docker.com/engine/install/ubuntu/)
3. Install and setup the database schema by following the [MySql instructions](https://github.com/ThePICARDProject/database-schema-backend/blob/main/README.md)
4. Clone the repository in the desired directory `git clone https://github.com/ThePICARDProject/API-backend`
5. Follow steps 1-4 in the [DockerSwarm C# Quick Start](/Services/DockerSwarm/dockerswarm.md) to set up the DockerSwarm service.
6. Navigate to `./bin/Debug/net8.0` and execute `dotnet API-backend.dll`
7. The backend service should run and begin initializing Docker Swarm.

#### Integration with Front End Applications

Currently the PICARD server does not have an open port for making requests. Integration should be achieved through an SSH tunnel to the server. In order to utilize the PICARD server, users must have ssh access to the WVU SSH Gateway, the PICARD Server, and have a user profile on the server. An SSH tunnel can be opened using the following steps:

1. From the machine running the front end, open a terminal and execute `ssh -L 5080:localhost:5080 <username>@ssh.wvu.edu`
2. After creating a tunnel to the ssh gateway, tunnel from the gateway to the PICARD server by executing `ssh -L 5080:localhost:5080 <username>@157.182.194.132`
3. The tunnel should now allow the server to be accessed through `https://localhost:5080/` on the front end application.

#### Common Errors

## API Documentation

### API-backend
### Version: 1.0

### /api/algorithms/algorithms

#### GET
##### Summary:

Gets all of a users algorithms stored in the database

##### Description:

Gets all of a users algorithms stored in the database

#### Request Data

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

```
[
  {
    "algorithmId": 0,
    "algorithmName": "string"
  }
]
```

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

```
[
  {
    "parameterId": 0,
    "parameterName": "string",
    "driverIndex": 0,
    "dataType": "string"
  }
]
```

### /api/algorithms/upload

#### POST
##### Summary:

Uploads a users algorithm to the database

##### Parameters

| Name | Located in | Description | Required | Schema |
| ---- | ---------- | ----------- | -------- | ---- |
| AlgorithmName | query | name of the algorithm that the user is uploading | Yes | string |
| MainClassName | query | main scala class containing the driver code | Yes | string |
| AlgorithmType | query | type of algorithm | Yes | integer |
| JarFile | query | jar file containing the packaged driver and algorithm implementation | YEs | file |
| Parameters | query | array containing definitions for the algorithm parameters | Yes | json array |

#### Algorithm Type Codes

| Value | Type |
| ----- | ---- |
| 0 | Supervised |
| 1 | Unsupervised |
| 2 | Semi-Supervised |

### Parameters Json Array

```
[
  {
    "parameterName": "string",
    "driverIndex": 0,
    "dataType": "string"
  }
}
```

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success 

# TODO: Check on this

```
{
  "algorithmId": 0
}
```

### /Authentication/login

#### GET
##### Summary:

Redirects user to the Google OAuth page for authentication

##### Parameters

| Name | Located in | Description | Required | Schema |
| ---- | ---------- | ----------- | -------- | ---- |
| returnUrl | query | The Url to redirect to when Google OAuth2.0 completes | No | string |

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
| returnUrl | query | The Url to redirect to | No | string |

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

### /api/dataset

#### GET
##### Summary:

Gets all of a users dataset information

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

```
[
  {
    "dataSetID": 0,
    "name": "string",
    "description": "string",
    "filePath": "string",
    "uploadedAt": "string",
    "downloadUrl": "string"
  }
]
```

### /api/dataset/{id}

#### GET
##### Summary:

Gets a dataset from a dataset id

##### Parameters

| Name | Located in | Description | Required | Schema |
| ---- | ---------- | ----------- | -------- | ---- |
| id | path |  | Yes | integer |

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

```
{
  "dataSetID": 0,
  "name": "string",
  "description": "string",
  "filePath": "string",
  "uploadedAt": "string",
  "downloadUrl": "string"
}
```

### /api/dataset/download/{id}

#### GET
##### Summary:

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

#### Parameters

| Name | Located in | Description | Required | Schema |
| ---- | ---------- | ----------- | -------- | ---- |
| File | query |  | Yes | file |
| Name | query |  | Yes | string |
| Description | query |  | Yes | string |

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

```
{
  "message": "string",
  "dataSetID": 0
}
```

### /api/experiment/submit

#### POST
##### Summary:

Submits an experiment to the cluster

#### Parameters

```
{
  "experimentName": "string",
  "algorithmId": 0,
  "datasetName": "string",
  "nodeCount": 0,
  "driverMemory": "string",
  "driverCores": 0,
  "executorNumber": 0,
  "executorCores": 0,
  "executorMemory": "string",
  "memoryOverhead": 0,
  "parameterValues": [
    {
      "parameterId": 0,
      "value": "string"
    }
  ]
}
```

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

```
{
  "message": "string",
  "experimentId": "string"
}
```

### /api/experiment/status/{experimentId}

#### GET
##### Summary:

Gets the status of a submitted experiment

##### Parameters

| Name | Located in | Description | Required | Schema |
| ---- | ---------- | ----------- | -------- | ---- |
| experimentId | path |  | Yes | string (uuid) |

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

#### TODO: CHECK ON RESPONSE JSON

### /api/result/getProcessedResults/{aggregateDataId}

#### GET
##### Summary:

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

#### Parameters:

```
{
  "clusterParameters": [
    {
      "clusterParameterName": "string",
      "operator": "string",
      "value": "string"
    }
  ],
  "algorithmParameters": [
    {
      "algorithmParameterId": 0,
      "operator": "string",
      "value": "string"
    }
  ]
}
```

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

```
{
  "aggregateDataId": 0
}
```

### /api/result/createCsv

#### POST
##### Summary:

Creates a .csv file for an aggregated data result based on its id

##### Description:

Creates a .csv file for an aggregated data result based on its id. The metrics identifiers must match the raw data files exactly, and the identifier and the value must be seperated by a single equals sign.

#### Parameters

```
{
  "aggregateDataId": 0,
  "metricsIdentifiers": [
    "string"
  ]
}
```

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

```
{
  "csvResultId": 0
}
```

### /api/result/DockerSwarmParams

#### GET
##### Summary:

Gets all cluster parameters in the database

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

```
[
  {
    "clusterParamID": 0,
    "experimentID": "string",
    "nodeCount": 0,
    "driverMemory": "string",
    "driverCores": 0,
    "executorNumber": 0,
    "executorCores": 0,
    "executorMemory": "string",
    "memoryOverhead": 0,
    "experimentRequest": "string"
  }
]
```

### /User/userinfo

#### GET
##### Summary:

Gets user info

##### Responses

| Code | Description |
| ---- | ----------- |
| 200 | Success |

```
{
  "firstName": "string",
  "lastName": "string",
  "email": "string",
  "userID": "string"
}
```

### /api/Visualization

#### POST
##### Summary:

Not Currently Working

## Current State/Future Work

#### Current State

The PICARD API/backend currently implements the following features:
1. User Authentication using Google OAuth2.0
2. Storing users datasets, algorithms, results in the database
3. Experiment submission through an API request
4. Generating Csv results from raw data
5. Downloading results from the backend database and filesystem

#### Future Work

The following features/improvements should continue to be developed:
1. Implemented features need to be thoroughly tested and verified
2. Successful integration of the visualization service
3. Enhanced delete/purge functionalities
4. Implement/Integrate resource management and montoring tools
5. Expand the cluster to run experiments in parallel
6. Implement advanced run configurations to execute multiple experiments in series for a single request 

## Help/Resources

1. For more information on the Docker Swarm service and wrapper class see the [documentation]().
2. For database documentation see [database-schema-backend]()
3. For resources on C# and .NET see the [Microsoft Learn Documentation]()
4. For more resources about Docker see the [Docker Documentation]()
