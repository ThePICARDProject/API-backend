/**********************************************************************************
* CoDRIFt.scala
* @author zennisarix
* This is a driver for experiments using the CoDRIFtModel with varying
* parameter values passed via command-line arguments using spark-submit. The
* specified dataset must be fully labeled. This code splits that dataset into
* four disjoint sets: the labeled set, unlabeled set, evaluation set for
* CoDRIFt's internal evaluations, and the final testing set for testing the
* final CoDRIFt model. The number of instances in each set are user-specified
* through the command line arguments. Results of the final evaluation only are
* saved to the specified file.
******************************************************************************/
package edu.fsu.driver

import edu.fsu.tree.model.CoDRIFtModel
import org.apache.spark.SparkContext
import org.apache.spark.SparkContext._
import org.apache.spark.SparkConf
import org.apache.spark.rdd.RDD
import org.apache.spark.mllib.linalg.Vectors
import org.apache.spark.mllib.evaluation.MulticlassMetrics
import org.apache.spark.mllib.regression.LabeledPoint
import java.io.StringWriter
import scala.collection.Seq
import scala.collection.mutable.ListBuffer
import scala.collection.mutable.Map
import scala.collection.immutable.Map
import org.apache.spark.HashPartitioner
import org.apache.hadoop.mapreduce.lib.chain.Chain.KeyValuePair

object CoDRIFt {
  def main(args: Array[String]) {
    if (args.length < 8) {
      System.err.println("Must supply valid arguments: [numClasses] [numTrees] " +
        "[impurity] [maxDepth] [maxBins] [input filename] [output filename] " +
        "[percent labeled] [numExecutors] [k]")
      System.exit(1)
    }
    // setup parameters from command line arguments
    val inFile = args(0)
    val outPath = args(1)
    val outName = args(2)
    val numClasses = args(3).toInt
    val numTrees = args(4).toInt
    val impurity = args(5)
    val maxDepth = args(6).toInt
    val maxBins = args(7).toInt
    /* CORRECTION: fix master url, make output file path consistent with SupervisedMLRF */
    val outFile = outPath + "/" + outName
    val percentLabeled = args(8).toDouble * 0.01
    val k = args(9).toInt * 2
    // initialize spark
    val sparkConf = new SparkConf().setAppName("SupervisedMLRF")
    val sc = new SparkContext(sparkConf)

    // configure hdfs for output
    val hadoopConf = new org.apache.hadoop.conf.Configuration()
    val hdfs = org.apache.hadoop.fs.FileSystem.get(
      /* CORRECTION: fix master url */
      new java.net.URI(outPath), hadoopConf)
    val out = new StringWriter()

/******************************************************************************
* Read in data and prepare for sampling
******************************************************************************/

    // load data file from hdfs
    val text = sc.textFile(inFile)

    // remove header line from input file
    val textNoHdr = text.mapPartitionsWithIndex(
      (i, iterator) => if (i == 0) iterator.drop(1) else iterator)

    // parse input text from CSV to RDD and convert to KVPRDD
    val kvprdd = transformCSV2KVPs(textNoHdr.map(line => line.split(",")))

/******************************************************************************
* Create training and testing sets.
******************************************************************************/

    // Split the data into training and test sets (30% held out for testing)
    val startTimeSplit = System.nanoTime
    val (modelbuildingKVP, testingKVP) = stratifiedRandomSplit(kvprdd, 0.7)
    // Pull out another 30% for evaluation of models during training
    val (trainingKVP, evaluationKVP) = stratifiedRandomSplit(modelbuildingKVP, 0.7)
    // Create labeled and unlabeled sets.
    val (labeledKVP, unlabeledKVP) = stratifiedRandomSplit(trainingKVP, percentLabeled)
    val splitTime = (System.nanoTime - startTimeSplit) / 1e9d
    val labeledLP = transformKVPs2LabeledPoints(labeledKVP)
    val unlabeledLP = transformKVPs2UnlabeledPoints(unlabeledKVP)
    val evaluationLP = transformKVPs2LabeledPoints(evaluationKVP)
    labeledLP.repartition(k)
    labeledLP.persist()
    unlabeledLP.repartition(k)
    unlabeledLP.persist()
    evaluationLP.persist()

/******************************************************************************
* Create the inductive CoDRIFt model*
******************************************************************************/

    val startTimeTrain = System.nanoTime
    val codrift = new CoDRIFtModel(labeledLP, unlabeledLP, evaluationLP, k, 75, 10,
      impurity, maxDepth, maxBins)
    val trainTime = (System.nanoTime - startTimeTrain) / 1e9d

/******************************************************************************
* Test the model on the held-out testing data.
******************************************************************************/

    val startTimeTest = System.nanoTime
    val testingLP = transformKVPs2LabeledPoints(testingKVP)
    val labelAndPreds = codrift.predict(testingLP)
    val testTime = (System.nanoTime - startTimeTest) / 1e9d

/******************************************************************************
* Metrics calculation for classification and execution performance
*  evaluation of the CoDRIFt model on unseen data.
******************************************************************************/

    val metrics = new MulticlassMetrics(labelAndPreds)
    out.write("EXECUTION PERFORMANCE:\n")
    out.write("SplittingTime=" + splitTime + "\n")
    out.write("TrainingTime=" + trainTime + "\n")
    out.write("TestingTime=" + testTime + "\n\n")

    out.write("CLASSIFICATION PERFORMANCE:\n")
    // Confusion matrix
    out.write("Confusion matrix (predicted classes are in columns):\n")
    out.write(metrics.confusionMatrix + "\n")

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

    // Weighted stats
    out.write(s"\nWeighted precision: ${metrics.weightedPrecision}\n")
    out.write(s"Weighted recall: ${metrics.weightedRecall}\n")
    out.write(s"Weighted F1 score: ${metrics.weightedFMeasure}\n")
    out.write(s"Weighted false positive rate: ${metrics.weightedFalsePositiveRate}\n")
    out.write(s"\nLearned classification forest model:\n")
    // output trees
    var count = 1
    for ((source, tree, score, label) <- codrift.trees) {
      out.write("\nTREE " + count + ": SOURCE=" + source + " LABEL=" + label + " RECALL=" + score + "\n")
      out.write(tree.toDebugString)
      count += 1
    }

    // output training and testing data
    //    println("TRAINING DATA:\n")
    //    trainingData.collect().map(println)
    //    println("TESTING DATA:\n")
    //    testingData.collect().map(println)

    // delete current existing file for this model
    try {
      hdfs.delete(new org.apache.hadoop.fs.Path(outFile), true)
    } catch {
      case _: Throwable => { println("ERROR: Unable to delete " + outFile) }
    }

    // write string to file
    val outRDD = sc.parallelize(Seq(out.toString()))
    outRDD.saveAsTextFile(outFile)
    sc.stop()
  }
/******************************************************************************
 * Converts an RDD of string arrays into an RDD of key-value pairs.
 * rdd: the dataset to convert, read in from a CSV
 * Returns: RDD of key-value pairs, with the key as the last value in a row
 *     and the value as everything before the key
******************************************************************************/
  def transformCSV2KVPs(
    rdd: RDD[Array[String]]): RDD[(Int, scala.collection.immutable.IndexedSeq[Double])] = {
    rdd.map(row => (
      row.last.toInt,
      (row.take(row.length - 1).map(str => str.toDouble)).toIndexedSeq))
  }
/******************************************************************************
 * Converts an RDD of key-value pairs into an RDD of LabeledPoints.
 * @param rdd the dataset to convert
 * @return RDD of LabeledPoints, with the label as the key
******************************************************************************/
  def transformKVPs2LabeledPoints(
    kvPairs: RDD[(Int, scala.collection.immutable.IndexedSeq[Double])]): RDD[LabeledPoint] = {
    kvPairs.map(pair => new LabeledPoint(
      pair._1,
      Vectors.dense(pair._2.toArray)))
  }
/******************************************************************************
 * Converts an RDD of key-value pairs into an RDD of LabeledPoints with all
 *   labels equal to -1.
 * @param rdd the dataset to convert
 * @return RDD of LabeledPoints, with the label as the key
******************************************************************************/
  def transformKVPs2UnlabeledPoints(
    kvPairs: RDD[(Int, scala.collection.immutable.IndexedSeq[Double])]): RDD[LabeledPoint] = {
    kvPairs.map(pair => new LabeledPoint(
      -1,
      Vectors.dense(pair._2.toArray)))
  }
/******************************************************************************
 * Splits a key-value pair dataset into two stratified sets. The size of
 * the sets are user-determined.
 *   @param rdd the dataset to split
 *  @param trainPercent the size in percent of the training set
 *  @return two key-value paired RDDs of LabeledPoints
******************************************************************************/
  def stratifiedRandomSplit(
    kvPairs:      RDD[(Int, scala.collection.immutable.IndexedSeq[Double])],
    trainPercent: Double): (RDD[(Int, scala.collection.immutable.IndexedSeq[Double])], RDD[(Int, scala.collection.immutable.IndexedSeq[Double])]) = {
    // set the size of the training set
    val fractions = scala.collection.immutable.Map(1 -> trainPercent, 0 -> trainPercent)
    // get a stratified random subsample from the full set
    val train = kvPairs.sampleByKeyExact(false, fractions, System.nanoTime())
    // remove the elements of the training set from the full set
    val test = kvPairs.subtract(train)
    (train, test)
  }
}
