# Overview
This is a driver for experiments using the CoDRIFtModel with varying parameter values passed via command-line arguments using spark-submit. The specified dataset must be fully labeled. This code splits that dataset into four disjoint sets: the labeled set, unlabeled set, evaluation set for CoDRIFt's internal evaluations, and the final testing set for testing the final CoDRIFt model. The number of instances in each set are user-specified through the command line arguments. Results of the final evaluation only are saved to the specified file.

# Methods
## transformCSV2KVPs method 
Creates a list of k (a user-defined value) RandomForest models from randomly selected samples of the labeled data set. The number of samples selected make up 1/kth of the size of the data set. It is important to note that these subsamples are NOT stratified, and may not be representative of the distribution of the class value. This was done deliberately, so that some RandomForests may be trained on only negative examples in an imbalanced data set, as a classifier for imbalanced data must be very good at classifying negative examples with a high recall. In repeated trials during development, we noticed that CoDRIFt classifiers that only contained trees good at identifying positive examples routinely under-performed on imbalanced unseen data sets.

### Parameters
#### data
a fully labeled RDD[LabeledPoint] for training

### Return
a list of RandomForests trained on the provided data
	

## transformKVPs2LabeledPoints method
Converts an RDD of key-value pairs into an RDD of LabeledPoints.

### Parameters

#### rdd
The dataset to convert

### Return
RDD of LabelePoints, with the label as the key.

## transformKVPs2UnlabeledPoints method
Converts an RDD of key-value pairs into an RDD of LabeledPoints with all
### Parameters

#### rdd
the dataset to convert

### Return
RDD of LabeledPoints, with the label as the key


## stratifiedRandomSplit method

### Parameters
#### rdd
the database to split
#### trainPercent
the size in percent of the training set
### Return
two key-value paired RDDs of LabeledPoints

