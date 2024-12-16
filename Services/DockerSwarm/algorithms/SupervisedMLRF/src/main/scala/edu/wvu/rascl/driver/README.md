# Overview
The SupervisedMLRF scala file was originally authored by @zennisarix.
The code implements a standard Spark mllib RandomForest classifier on the dataset provided with parameters passed via command-line arguments. The specified dataset must be fully labeled. The RandomForest model is trained on the training data, used to make predictions on the testing data, and evaluated for classification performance. The calculated metrics and model are sent to the hdfs with the provided filename.
