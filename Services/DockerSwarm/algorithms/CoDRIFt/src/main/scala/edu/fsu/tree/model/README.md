# Overview


# CoDRIFtModel Class Info
a novel algorithm to create inductive Co-Training, Distributed, Random Incremental Forest (CoDRIFt) models for classification on distributed systems. CoDRIFt iteratively creates an inductive CoDRIFt model. The CoDrift model is a collection of trees that operates much like a RandomForest, with two key differences: 1) each tree is created by a different RandomForest model during training and selected as the ``best tree'', and 2) when making a prediction, a CoDRIFt model weights predictions for the positive class three times more than predictions for the negative class. This form of weighted voting allows CoDRIFt models dealing with highly imbalanced data to better identify the minority class (i.e., achieve higher recall) at the expense of lower precision. This trade-off is acceptable for extremely rare instance classification , as the cost of missing a rare instance (false negative) is much higher than that of having an extra false positive.

## Parameters

### labeled
a fully labeled RDD[LabeledPoint] for training RandomForests

### unlabeled  
n unlabeled RDD[LabeledPoint] (all labels = "-1.0")
### evaluation
a fully labeled RDD[LabeledPoint] for evaluating RandomForests
### k
The number of partitions.
### numTreesCD
The minimum number of trees to create for the final model.
###numTreesRF
 The number of trees for the RandomForests
###impurity
Criterion used for information gain calculation.
Supported values: "gini" (recommended) or "entropy".
###maxDepth
Maximum depth of the tree (e.g. depth 0 means 1 leaf node, depth 1 means 1 internal node + 2 leaf nodes). (suggested value: 4)

###maxBins
 Maximum number of bins used for splitting features (suggested value: 100)

# Methods
## trainRFs method 
Creates a list of k (a user-defined value) RandomForest models from randomly selected samples of the labeled data set. The number of samples selected make up 1/kth of the size of the data set. It is important to note that these subsamples are NOT stratified, and may not be representative of the distri- bution of the class value. This was done deliberately, so that some RandomForests may be trained on only negative examples in an imbalanced data set, as a classifier for imbalanced data must be very good at classifying negative examples with a high recall. In repeated trials during development, we noticed that CoDRIFt classifiers that only contained trees good at identifying positive examples routinely under-performed on imbalanced unseen data sets.

### Parameters
#### data
a fully labeled RDD[LabeledPoint] for training

### Return
a list of RandomForests trained on the provided data
	

## evaluateModels method
Creates a list of k (a user-defined value) RandomForest models from randomly selected samples of the labeled data set. The number of samples selected make up 1/kth of the size of the data set. It is important to note that these subsamples are NOT stratified, and may not be representative of the distri- bution of the class value. This was done deliberately, so that some RandomForests may be trained on only negative examples in an imbalanced data set, as a classifier for imbalanced data must be very good at classifying negative examples with a high recall. In repeated trials during development, we noticed that CoDRIFt classifiers that only contained trees good at identifying positive examples routinely under-performed on imbalanced unseen data sets.
#### Parameters
#### models
the list of RandomForest models to be evaluated
### Return
a String indicating whether the models under evaluation are from supervised, or semi-supervised RandomForests

## createAugmentedSet method
Creates an augmented that includes both the initial labeled set and all of the high-confidence predictions from the supervised RandomForests created in step one. To accomplish this, the unlabeled data is first divided into k partitions. Then, in parallel, a random supervised RandomForest model is chosen to predict labels for the unlabeled data in each partition. If a prediction has a confidence >= 0.95, that instance is added to the augmented set with its new label.

### Parameters
#### rfModels
the list of RandomForest models to test.
### Return
an augmented list consisting of the original, labeled list with added high-confidence predictions from the unlabeled data set.

## predict method
Performs weighted voting on each instance in the given, unseen test set using this CoDRIFt model. The predicted class is the majority vote from the CoDRIFt trees, with votes for the minority class weighted 3x more than votes for the majority class.

### Parameters
#### testSet
an unseen, fully labeled RDD[LabeledPoint] for evaluation of this CoDRIFt model.
### Return
an RDD[(Double, Double)] for use by a MulticlassMetrics object, where the first Double is the actual class value and the second double is the predicted class value.























