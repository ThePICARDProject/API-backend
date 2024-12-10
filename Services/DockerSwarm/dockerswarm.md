# Docker Swarm C# Wrapper

## Overview

The DockerSwarm C# wrapper attempts to provide an easy interface to automate and integrate ThePICARDProject Docker Swarm backend service to integrate with C# applications. 
The DockerSwarm.cs implementation provides methods for managing DockerSwarm and submitting experiments to the cluster without having to manually run experiments with
interactive scripts.

## Setup

### Driver Specification

The Docker Swarm backend application submits machine learning experiments to Spark using a packaged scala programs in the form of a .jar file. One of these files will be the driver code which will be the main
program run on the cluster, and as such, it is important that this driver code allows for integration with Docker Swarm on the PICARD server. When impementing driver code the following requirements must be considered:

#### 1. Positional Arguments
  1. When a .jar file is submitted to spark using `spark-submit` the arguments for the driver program will be passed by position. In order for the driver code to be submitted, The first three arguments MUST be reserved for
the hadoop URL, output directory, and output file name. After an experiment is run these might change, however they are essential for the driver to be able to interact with hadoop.

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
     
#### 2. Output File Formats
  1. Results from driver code must be writted to a file defined by the paths provided as arguments. In order to facilitate the process of extracting results in various applications, the results must be in a particular format

      ##### Example Code:
      In the PICARD backend, results will be parsed based on a provided name for a specific metric and an `=` character. It is essential that results are written to the output file with the required format
      in order to get results.
    
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
      
### File Dependencies

The Docker Swarm C# wrapper also is dependent on a number of scripts and other files in order to run experiments. The files/directories are as follows:
  
  1. `./docker-compose.yml`
  2. `./scripts`
  3. `./docker-images`

In order to instantiate the DockerSwarm object these files/directories must be copied into the application root directory using the commands `cp <file_name> <root_directory>` for files and `cp <file_name> <root_directory> -r`
for directories.

### Software Dependencies

The DockerSwarm wrapper requires the following to be installed:

##### 1. Docker
  1. View the official Docker Install Documentation to install Docker.
  2. If using WSL, install Docker Deskop and enable integration with your WSL distribution.
##### 2. ACL
  1. The backend uses ACL file permissions, so it is required that the linux environment running the service has the ACL package installed.
  2. Install by running `sudo apt update` and `sudo apt install acl`.   

### Operating System

The Docker Swarm backend is designed to run in a linux environment, so the DockerSwarm wrapper cannot be used natively in Windows or MAC. Attempting to use the service in an unsupported environment will 
result in exceptions when trying to instantiate the service.

## QuickStart

In order to get started using the DockerSwarm service, follow these instructions:

1. In a linux terminal, add the user the application will be running under to the docker group
  1. Run `usermod -aG docker <username>`
2. Open a new shell.
  1. Run `newgrp`
3. In program code, instantiate the DockerSwarm object
   1. Using the default constructor:
      1. ` DockerSwarm dockerSwarm = new DockerSwarm(<applicationRootDirectory); `
4. Execute an experiment using the submit method:
   1. ExperimentResponse response = await dockerSwarm.SubmitExperiment(<experimentRequestData>, <dataset>);
5. Validate script execution by validating the properties returned in the `ExperimentResponse` object
6. For more in-depth code documentation or resources for further reading, see the Detailed Documentation/More Information

## Detailed Documentation/More Information

1. DockerSwarm Full Documentation:
2. The PICARD project API documentation: 
3. Docker Documentation:
4. Hadoop Documentaion:
5. Spark Documentation:
6. Microsoft Learn Documenation:
