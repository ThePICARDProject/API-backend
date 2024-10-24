# Overview
The RandomForest scala file contains several classes used in [random forest ML algorithms](https://en.wikipedia.org/wiki/Random_forest).

# RandomForest Class Info
A random forest learning algorithm for classification and regression. It supports both continuous and categorical features.

The settings for featureSubsetStrategy are based on the following references:
- log2: tested in Breiman (2001)
- sqrt: recommended by Breiman manual for random forests
- The defaults of sqrt (classification) and onethird (regression) match the R randomForest package.

## References 
[Breiman (2001)](http://www.stat.berkeley.edu/~breiman/randomforest2001.pdf)
[Breiman manual for random forests](http://www.stat.berkeley.edu/~breiman/Using_random_forests_V3.1.pdf)

## Parameters
### stragegy
The configuration parameters for the random forest algorithm which specify the type of random forest (classification or regression), feature type (continuous, categorical), depth of the tree, quantile calculation strategy, etc.
  
### numtrees  
If 1, then no bootstrapping is used.  If greater than 1, then bootstrapping is                done.

### featureSubsetStrategy
Number of features to consider for splits at each node.
> [!NOTE]
> Supported values: "auto", "all", "sqrt", "log2", "onethird". Supported numerical values: "(0.0-1.0]", "[1-n]".

1. If "auto" is set, this parameter is set based on numTrees:
	- If numTrees == 1, set to "all";
	- If numTrees is greater than 1 (forest) set to "sqrt" for classification and to "onethird" for regression.
1. If a real value "n" in the range (0, 1.0] is set, use n  number of features.
1. If an integer value "n" in the range (1, num features) is set use n features.

### seed
Random seed for bootstrapping and choosing feature subsets.

# Methods and objects
## stragegy.assertValid method
Method to train a decision tree model over an RDD
### input parameter
Training data: RDD of _org.apache.spark.mllib.regression.LabeledPoint_. Returns RandomForestModel that can be used for prediction.
[!WARNING]
Method signature will need to be fixed for newer versions of spark.
	
## RandomForest object
Method to train a decision tree model for binary or multiclass classification.
### Parameters
#### input
Training dataset: RDD of _org.apache.spark.mllib.regression.LabeledPoint_. Labels should take values {0, 1, ..., numClasses-1}.
#### strategy
Parameters for training each tree in the forest.
#### numTrees
Number of trees in the random forest. param featureSubsetStrategy Number of features to consider for splits at each node. 
> [!NOTE]
> Supported values: "auto", "all", "sqrt", "log2", "onethird".
1. If "auto" is set, this parameter is set based on numTrees:
	- If numTrees == 1, set to "all";
	- If numTrees is greater than 1 (forest) set to "sqrt".
#### seed 
Random seed for bootstrapping and choosing feature subsets. Returns RandomForestModel that can be used for prediction.

## trainClassifier method
Method to train a decision tree model for binary or multiclass classification.
### Parameters
####input
Training dataset: RDD of _org.apache.spark.mllib.regression.LabeledPoint_. Labels should take values {0, 1, ..., numClasses-1}.
#### numClasses 
Number of classes for classification.
#### categoricalFeaturesInfo
Map storing arity of categorical features. An entry (n to k)indicates that feature n is categorical with k categories indexed from 0: {0, 1, ..., k-1}.
#### numTrees
Number of trees in the random forest.
#### featureSubsetStrategy 
Number of features to consider for splits at each node. 
> [!NOTE]
> Supported values: "auto", "all", "sqrt", "log2", "onethird".
1. If "auto" is set, this ####eter is set based on numTrees:
	- If numTrees == 1, set to "all";
	- If numTrees is greater than 1 (forest) set to "sqrt".
#### impurity 
Criterion used for information gain calculation. Supported values: "gini" (recommended) or "entropy".
#### maxDepth 
Maximum depth of the tree (e.g. depth 0 means 1 leaf node, depth 1 means 1 internal node + 2 leaf nodes). (suggested value: 4)
#### maxBins 
Maximum number of bins used for splitting features (suggested value: 100)
#### seed 
Random seed for bootstrapping and choosing feature subsets. Returns RandomForestModel that can be used for prediction.

## trainRegressor method
> [!NOTE]
> Overloading is used for this method.
This is a method to train a decision tree model for regression, unless otherwise stated in the overload.
### Original Parameters
#### input
Training dataset: RDD of _org.apache.spark.mllib.regression.LabeledPoint_. Labels are real numbers.
#### strategyeters 
Used for training each tree in the forest.
#### numTrees 
Number of trees in the random forest.
#### featureSubsetStrategy 
Number of features to consider for splits at each node. 
> [!NOTE]
> Supported values: "auto", "all", "sqrt", "log2", "onethird".
1. If "auto" is set, thiseter is set based on numTrees:
	- If numTrees == 1, set to "all";
	- If numTrees is greater than 1 (forest) set to "onethird".
#### seed 
Random seed for bootstrapping and choosing feature subsets. Return RandomForestModel that can be used for prediction.

### First Overloadeters
#### input Training dataset: RDD of [[org.apache.spark.mllib.regression.LabeledPoint]].
Labels are real numbers.
#### categoricalFeaturesInfo 
Map storing arity of categorical features. An entry (n to k) indicates that feature n is categorical with k categories indexed from 0: {0, 1, ..., k-1}.
#### numTrees 
Number of trees in the random forest.
#### featureSubsetStrategy 
Number of features to consider for splits at each node. 
> [!NOTE]
> Supported values: "auto", "all", "sqrt", "log2", "onethird".
1. If "auto" is set, thiseter is set based on numTrees:
	- If numTrees == 1, set to "all";
	- If numTrees is greater than 1 (forest) set to "onethird".
#### impurity Criterion used for information gain calculation.
The only supported value for regression is "variance".
#### maxDepth 
Maximum depth of the tree. (e.g., depth 0 means 1 leaf node, depth 1 means 1 internal node + 2 leaf nodes). (suggested value: 4)
#### maxBins 
Maximum number of bins used for splitting features. (suggested value: 100)
#### seed 
Random seed for bootstrapping and choosing feature subsets. Returns RandomForestModel that can be used for prediction.
 
### Second Overload
A Java-friendly API for _org.apache.spark.mllib.tree.RandomForest.trainRegressor_
