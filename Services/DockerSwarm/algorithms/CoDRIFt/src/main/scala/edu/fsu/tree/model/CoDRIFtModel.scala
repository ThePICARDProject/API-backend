/******************************************************************************
* CoDRIFt.scala
* @author zennisarix
* This is the implementation of CoDRIFt, a novel algorithm to create inductive
* Co-Training, Distributed, Random Incremental Forest (CoDRIFt) models for
* classification on distributed systems. CoDRIFt iteratively creates an
* inductive CoDRIFt model. The CoDrift model is a collection of trees that
* operates much like a RandomForest, with two key differences:
* 1) each tree is created by a different RandomForest model during training
* and selected as the ``best tree'', and
* 2) when making a prediction, a CoDRIFt model weights predictions for the
* positive class three times more than predictions for the negative class.
* This form of weighted voting allows CoDRIFt models dealing with highly
* imbalanced data to better identify the minority class (i.e., achieve higher
* recall) at the expense of lower precision. This trade-off is acceptable for
* extremely rare instance classification , as the cost of missing a rare
* instance (false negative) is much higher than that of having an extra false
* positive.
******************************************************************************/

package edu.fsu.tree.model

import scala.collection.mutable.ListBuffer
import org.apache.spark.rdd.RDD
import org.apache.spark.mllib.evaluation.MulticlassMetrics
import org.apache.spark.mllib.regression.LabeledPoint
import org.apache.spark.mllib.tree.RandomForest
import org.apache.spark.mllib.tree.model.RandomForestModel
import org.apache.spark.mllib.tree.model.DecisionTreeModel

/******************************************************************************
* CoDRIFt operates in rounds, only stopping when the model has collected the
* user-specified number of trees. Each round consists of five steps. The end
* result is an inductive semi-supervised CoDRIFt model containing the best
* supervised and semi-supervised trees from many different supervised and semi-
* supervised RandomForest models. The main novel aspects of CoDRIFt that set it
* apart from the other algorithms in the literature are:
*    - We create many RandomForest models and keep only the best decision trees
*      from each model. We place an emphasis on composing an ensemble of
*      stronger learners, rather than training learners on larger data sets
*      with more labels.
*    - Our final model contains a mix of supervised trees (trained only on
*      labeled data) and semi-supervised trees (trained on data augmented with
*      labels from other RandomForest models).
*    - We optimize our model to achieve higher recalls by selecting trees with
*      high recalls on unseen data.
*    - We use weighted voting when making predictions, but count positive
*      predictions three times heavier than negative predictions instead of
*      weighting each individual tree. This helps our algorithm perform well in
*      imbalanced data sets.
*    - Each iteration, the unlabeled set resets, allowing instances to be
*      labeled multiple times by different trees in different iterations. This
*      means unlabeled instances will not necessarily be labeled the same every
*      time, and new trees could be trained on the differently labeled data.
* The CoDRIFt model is general enough to be used as a distributed, semi-
* supervised classifier in any large data set where the cost of labeling is
* prohibitive and is designed to work especially well in problem domains that
* deal with extremely imbalanced data, such as cybercrime or credit card fraud.
*
* @param labeled a fully labeled RDD[LabeledPoint] for training RandomForests
* @param unlabeled n unlabeled RDD[LabeledPoint] (all labels = "-1.0")
* @param evaluation a fully labeled RDD[LabeledPoint] for evaluating RandomForests
* @param k The number of partitions.
* @param numTreesCD The minimum number of trees to create for the final model.
* @param numTreesRF The number of trees for the RandomForests
* @param impurity Criterion used for information gain calculation.
*                 Supported values: "gini" (recommended) or "entropy".
* @param maxDepth Maximum depth of the tree (e.g. depth 0 means 1 leaf node, depth 1 means
*                 1 internal node + 2 leaf nodes).
*                 (suggested value: 4)
* @param maxBins Maximum number of bins used for splitting features
*                (suggested value: 100)
******************************************************************************/
class CoDRIFtModel(
  protected val labeled:    RDD[LabeledPoint],
  protected val unlabeled:  RDD[LabeledPoint],
  protected val evaluation: RDD[LabeledPoint],
  protected val k:          Integer,
  protected val numTreesCD: Integer,
  protected val numTreesRF: Integer,
  protected val impurity:   String,
  protected val maxDepth:   Integer,
  protected val maxBins:    Integer)
  extends Serializable {
  // Make the empty array of trees
  var trees = new ListBuffer[(String, DecisionTreeModel, Double, Double)]()
  run()
/******************************************************************************
*  Populates this CoDRIFt model with numTreesCD trees by iteratively performing
*  the 5 steps of the CoDRIFt algorithm.
******************************************************************************/
  private def run() = {
    while (trees.size < numTreesCD) {
      // Step 1: Train supervised models on the labeled data
      val supervisedRFs = trainRFs(labeled)
      // Step 2: Add the best individual trees from each supervised RF to the codrift model
      evaluateModels(supervisedRFs, "labeled")
      // Step 3: Use models to label unlabeled points by partition and add them to the augmented set
      var augmented = createAugmentedSet(supervisedRFs)
      augmented.persist()
      // Step 4: Train semi-supervised models on the augmented data
      val unsupervisedRFs = trainRFs(augmented)
      augmented.unpersist()
      // Step 5: Add the best individual trees from each smei-supervised RF to the codrift model
      evaluateModels(unsupervisedRFs, "augmented")
    }
  }

/******************************************************************************
* Creates a list of k (a user-defined value) RandomForest models from randomly
* selected samples of the labeled data set. The number of samples selected make
* up 1/kth of the size of the data set. It is important to note that these
* subsamples are NOT stratified, and may not be representative of the distri-
* bution of the class value. This was done deliberately, so that some
* RandomForests may be trained on only negative examples in an imbalanced data
* set, as a classifier for imbalanced data must be very good at classifying
* negative examples with a high recall. In repeated trials during development,
* we noticed that CoDRIFt classifiers that only contained trees good at
* identifying positive examples routinely under-performed on imbalanced unseen
* data sets.
*
* @param data a fully labeled RDD[LabeledPoint] for training
* @return a list of RandomForests trained on the provided data
******************************************************************************/
  private def trainRFs(
    data: RDD[LabeledPoint]): ListBuffer[RandomForestModel] = {
    var rfModels = new ListBuffer[RandomForestModel]()
    for (i <- 1 to k) {
      val sample = labeled.sample(false, 1.0 / k)
      if (sample.count() > 0) {
        val model = RandomForest.trainClassifier(sample, 2,
          scala.collection.immutable.Map[Int, Int](), numTreesRF, "auto", impurity,
          maxDepth, maxBins)
        rfModels += model
      }
    }
    rfModels
  }

/******************************************************************************
* Evaluates the given list of RandomForest models and adds the best trees to
* the CoDRIFt model. The best trees from each of the k RandomForest models are
* determined by evaluating them individually on the unseen evaluation set. For
* each RandomForest model, two trees have the opportunity to be selected for
* inclusion in the CoDRIFt model, but no trees are guaranteed a spot. Each
* model determines its "best" two trees, one for classifying positive instances
* and one for classifying negative instances. In this version of CoDRIFt,
* recall scores were used to determine the best trees, but this could be
* altered to consider other performance metrics, as the situation dictates. We
* set inclusion criteria at recall > 0.99 for negative trees (trees good at
* classifying negative examples) and recall > 0.9 for positive trees (trees
* good at classifying positive examples). We chose these values to maintain
* diversity among the trees in the CoDRIFt model when there are many more
* negative than positive examples, since good negative trees in imbalanced
* data sets are a dime a dozen.
*
* @param models the list of RandomForest models to be evaluated
* @param source a String indicating whether the models under evaluation are
*        from supervised, or semi-supervised RandomForests
******************************************************************************/
  private def evaluateModels(
    models: ListBuffer[RandomForestModel],
    source: String) = {
    for (model <- models) {
      var bestPosScore = 0.0
      var bestNegScore = 0.0
      var bestPosTree = model.trees(0)
      var bestNegTree = model.trees(0)
      val rfTrees = model.trees
      for (tree <- rfTrees) {
        val labelAndPreds = evaluation.map { point =>
          val prediction = tree.predict(point.features)
          (point.label, prediction)
        }
        labelAndPreds.persist()
        val metrics = new MulticlassMetrics(labelAndPreds)
        val negScore = metrics.recall(0.0)
        if (negScore > bestNegScore) {
          bestNegTree = tree
          bestNegScore = negScore
        }
        if (metrics.labels.size > 1) {
          val posScore = metrics.recall(1.0)
          if (posScore > bestPosScore) {
            bestPosTree = tree
            bestPosScore = posScore
          }
        }
        labelAndPreds.unpersist()
      }
      if (bestPosScore > 0.9)
        trees += ((source, bestPosTree, bestPosScore, 1.0))
      if (bestNegScore > 0.99)
        trees += ((source, bestNegTree, bestNegScore, 0.0))
    }
  }

/******************************************************************************
* Creates an augmented that includes both the initial labeled set and all of
* the high-confidence predictions from the supervised RandomForests created in
* step one. To accomplish this, the unlabeled data is first divided into k
* partitions. Then, in parallel, a random supervised RandomForest model is
* chosen to predict labels for the unlabeled data in each partition. If a
* prediction has a confidence >= 0.95, that instance is added to the augmented
* set with its new label.
*
* @param rfModels the list of RandomForest models to test
* @return an augmented list consisting of the original, labeled list with added
*         high-confidence predictions from the unlabeled data set
******************************************************************************/
  private def createAugmentedSet(
    rfModels: ListBuffer[RandomForestModel]): RDD[LabeledPoint] = {
    val rand = scala.util.Random
    var augmented = unlabeled.mapPartitionsWithIndex { (index, iter) =>
      val points = iter.toList
      val model = rfModels(rand.nextInt(rfModels.size))
      points.map { point =>
        if (model != null) {
          val (prediction, confidence) = model.predict(point.features)
          if (confidence >= 0.95)
            new LabeledPoint(prediction, point.features)
          else
            point
        } else
          point
      }.iterator
    }
    augmented = augmented.filter(_.label != -1)
    // combine labeled and augmented sets
    augmented.union(labeled)
    augmented.repartition(k)
  }

/******************************************************************************
* Performs weighted voting on each instance in the given, unseen test set using
* this CoDRIFt model. The predicted class is the majority vote from the CoDRIFt
* trees, with votes for the minority class weighted 3x more than votes for the
* majority class.
*
* @param testSet an unseen, fully labeled RDD[LabeledPoint] for evaluation of
*        this CoDRIFt model
* @return an RDD[(Double, Double)] for use by a MulticlassMetrics object, where
*         the first Double is the actual class value and the second double is
*         the predicted class value
******************************************************************************/
  def predict(
    testSet: RDD[LabeledPoint]): RDD[(Double, Double)] = {
    testSet.map { point =>
      val votes = scala.collection.mutable.Map.empty[Int, Double]
      for ((source, tree, score, label) <- trees) {
        val prediction = tree.predict(point.features).toInt
        votes(prediction) = votes.getOrElse(prediction, 0.0) + 3 * label
      }
      (point.label, votes.maxBy(_._2)._1.toDouble)
    }
  }
}
