# Docker Swarm C# Wrapper

## Contents
1. [Overview](#overview)
2. [Setup](#setup)
3. [Quick Start](#quickstart)
4. [Detailed Documentation/More Information](#detailed-documentationmore-information)

## Overview

The Docker Swarm C# wrapper attempts to provide an easy interface to automate and integrate The PICARD Project Docker Swarm backend service. 
The DockerSwarm.cs implementation provides methods for managing the Docker Swarm cluster and submitting experiments without having to manually run experiments through interactive scripts.

## Setup

### Driver Specification

The Docker Swarm backend application submits machine learning experiments to Spark using packaged scala programs in the form of a .jar file. One of these files will be the driver code which functions as the main application running on the cluster, and for this reason, it is important that this driver code allows for integration with Docker Swarm on the PICARD server. When implementing driver code the following requirements must be considered:

1. Positional Arguments 
   1. When a .jar file is submitted to spark using `spark-submit` the arguments for the driver program will be passed by position. In order for the driver code to be submitted, The first three arguments MUST be reserved forthe hadoop URL, output directory, and output file name. After an experiment is run these might change, however they are essential for the driver to be able to interact with hadoop.

    ##### Example Code:
    ```
    // Setup parameters from command line arguments

    // Reserved arguments
    val inFile = args(0)
    val outPath = args(1)
    val outName = args(2)

    // Other algorithm parameters
    val numClasses = args(3).toInt
    val numTrees = args(4).toInt 
    val impurity = args(5)
    val maxDepth = args(6).toInt
    val maxBins = args(7).toInt
    val outFile =  outPath + "/" + outName
    val percentLabeled = args(8).toDouble * 0.01
    
    // Initialize spark 
    val sparkConf = new SparkConf().setAppName("SupervisedMLRF")
    val sc = new SparkContext(sparkConf)

    // Configure hdfs for output
    val hadoopConf = new org.apache.hadoop.conf.Configuration()
    val hdfs = org.apache.hadoop.fs.FileSystem.get(
      new java.net.URI(outPath), hadoopConf
    )
    
    ```

2. Output File Formats
   1. Results from driver code must be written to a file defined by the paths provided as arguments. To support the process of extracting results, output files must be in a particular format.

      ##### Example Code:
      In the PICARD backend, results will be parsed based on a provided name for a specific metric and an `=` character. Results must be written to the output file with the required format
      to get results.
    
      ```
      // Overall Statistics
      val accuracy = metrics.accuracy
      out.write("\nSummary Statistics:\n")
      out.write(s"Accuracy = $accuracy\n")
    
      // Precision by label
      val labels = metrics.labels
      labels.foreach { l =>
        out.write(s"Precision($l) = " + metrics.precision(l) + "\n")
      }
    
      // Recall by label
      labels.foreach { l =>
        out.write(s"Recall($l) = " + metrics.recall(l) + "\n")
      }
    
      // False positive rate by label
      labels.foreach { l =>
        out.write(s"FPR($l) = " + metrics.falsePositiveRate(l) + "\n")
      }
    
      // F-measure by label
      labels.foreach { l =>
        out.write(s"F1-Score($l) = " + metrics.fMeasure(l) + "\n")
      }
      
      ```
      
### File Dependencies

The Docker Swarm C# wrapper is dependent on multiple scripts and other files to run experiments. The files/directories are as follows:
  
  1. `./docker-compose.yml`
  2. `./scripts`
  3. `./docker-images`

In order to instantiate the DockerSwarm object these files/directories must be copied into the application root directory using the commands `cp <file_name> <root_directory>` for files and `cp <file_name> <root_directory> -r` for directories.

### Software Dependencies

The DockerSwarm wrapper requires the following to be installed:

##### 1. Docker
  1. View the official Docker Install Documentation to install Docker.
  2. If using WSL, install Docker Desktop and enable integration with your WSL distribution.
##### 2. ACL
  1. The backend uses ACL file permissions, so it is required that the Linux environment running the service has the ACL package installed.
  2. Install by running `sudo apt-get update` and `sudo apt-get install acl`.   

### Operating System

The Docker Swarm backend is designed to run in a Linux environment, so the DockerSwarm wrapper cannot be used natively in Windows or MacOS. Attempting to use the service in an unsupported environment will 
result in exceptions when trying to instantiate the service.

## QuickStart

To get started using the DockerSwarm service, follow these instructions:

1. In a Linux terminal, add the user the application will be running under to the docker group:
  1. Run `usermod -aG docker <username>`
2. Open a new shell:
  1. Run `newgrp`
3. Follow the directions in [Setup](#setup) to configure dependencies.
4. Enable execute permissions for the scripts directory:
   1. run `chmod +rwx <contentRootDirectory>/scripts/*.sh`
6. In program code, instantiate the DockerSwarm object
   1. Using the default constructor:
      1. ` DockerSwarm dockerSwarm = new DockerSwarm(<applicationRootDirectory); `
7. Execute an experiment using the submit method:
   1. ExperimentResponse response = await dockerSwarm.SubmitExperiment(<experimentRequestData>, <dataset>);
8. Validate script execution by validating the properties returned in the `ExperimentResponse` object
9. For more in-depth code documentation or resources for further reading, see the Detailed Documentation/More Information

## Detailed Documentation
