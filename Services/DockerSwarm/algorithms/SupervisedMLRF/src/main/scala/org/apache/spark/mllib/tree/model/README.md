# Overview
The treeEnsembleModels scala file containts classes, objects, and methods needed for the ML model. 
> [!Note]
> Numerous methods are overides.

# Classes & Objects
## RandomForestModel Class
Represents a random forest model.

### Class Parameters
algo algorithm for the ensemble model, either Classification or Regression
trees tree ensembles
### save method parameters
#### sc
Spark context used to save model data.
#### path
Path specifying the directory in which to save this model. If the directory already exists, this method throws an exception.

## RandomForestModel Object
Returns Model instance.
### Object Parameters 
#### sc  
Spark context used for loading model files.
#### path  
Path specifying the directory to which the model was saved.

## GradientBoostedTreesModel Class
Represents a gradient boosted trees model.
### Class Parameters
#### algo 
Algorithm for the ensemble model, either Classification or Regression
#### trees 
Tree ensembles
#### treeWeights 
Tree ensemble weights

### save method parameters
#### sc  
Spark context used to save model data.
#### path  
Path specifying the directory in which to save this model. If the directory already exists, this method throws an exception.

### evaluateEachIteration method
Method to compute error or loss for every iteration of gradient boosting.
Returns an array with index i having the losses or errors for the ensemble containing the first i+1 trees.
#### Parameters
##### data 
RDD of _org.apache.spark.mllib.regression.LabeledPoint_.
##### loss 
Evaluation metric.

## GradientBoostedTreesMode Object
### computeInitialPredictionAndError method
Computes the initial predictions and errors for a dataset for the first iteration of gradient boosting. Returns an RDD with each element being a zip of the prediction and error
corresponding to every sample.
#### Parameters
##### data
Training data.
##### initTreeWeight
Learning rate assigned to the first tree.
##### initTree 
First DecisionTreeModel.
##### loss
Evaluation metric.


### updatePredictionError method
Updates a zipped predictionError RDD (as obtained with computeInitialPredictionAndError). Returns an RDD with each element being a zip of the prediction and error corresponding to each sample.
#### Prameters
##### data 
Training data.
##### predictionAndError 
PredictionError RDD
##### treeWeight
Learning rate.
##### tree
Tree using which the prediction and error should be updated.
##### loss
Evaluation metric.

### load method
Returns  Model instance
> [!NOTE]
> Override.
#### Parameters
##### sc  
Spark context used for loading model files.
##### path  
Path specifying the directory to which the model was saved.

# TreeEnsembleModel
Represents a tree ensemble model.
## Model Parameters
### algo 
Algorithm for the ensemble model, either Classification or Regression
### trees 
Tree ensembles
### treeWeights 
Tree ensemble weights
### combiningStrategy 
Strategy for combining the predictions, not used for regression.

## sealed Class
### predictBySumming method
Predicts for a single data point using the weighted sum of ensemble predictions. Returns predicted category from the trained model.
The features parameter is an array representing a single data point.

### predictByVoting method
Classifies a single data point based on (weighted) majority votes.

### predictConf method
Calculates the confidence of a predicted class from a single data point based on (weighted) majority votes. Confidence is measured as the number of votes received for the majority class. Returns a tuple with the probability and predicted category from the trained model. 
The features parameter is an array representing a single data point.

### predict methods
[!NOTE] There are overrides for this method.
#### Original instance
Predicts values for a single data point using the model trained. Returns predicted category from the trained model. 
The features parameter is an array representing a single data point.

#### First Override.
Predicts values for the given data set. Returns a RDD[Double] where each entry contains the corresponding prediction.
The features parameter is a RDD representing data points to be predicted.

#### Second Override
Java-friendly version of _org.apache.spark.mllib.tree.model.TreeEnsembleModel.predict_.

### toString override.
Prints a summary of the model.

### toDebugString
Prints the full model to a string.

### numTrees method
Gets number of trees in ensemble.

### totalNumNodes method
Gets total number of nodes, summed over all trees in the ensemble.

## TreeEnsembleModel Object
### EnsembleNodeData Class
Model data for model importexport. We have to duplicate NodeData here since Spark SQL does not yet support extracting subfields of nested fields; once that is possible, we can use something like:
-case class EnsembleNodeData(treeId: Int, node: NodeData), where NodeData is from DecisionTreeModel.

#### readMetadata method
Reads metadata from the loaded JSON metadata.

#### loadTrees method
Load trees for an ensemble, and return them in order.
##### Parameters
###### path 
Path to load the model from
###### treeAlgo 
Algorithm for individual trees (which may differ from the ensemble's algorithm).
