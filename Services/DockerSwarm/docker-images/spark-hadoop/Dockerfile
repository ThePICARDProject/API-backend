FROM apache/hadoop:3

RUN sudo curl -k -o spark-3.5.3-bin-without-hadoop.tgz https://dlcdn.apache.org/spark/spark-3.5.3/spark-3.5.3-bin-without-hadoop.tgz
RUN sudo tar zxvf spark-3.5.3-bin-without-hadoop.tgz
RUN sudo mv spark-3.5.3-bin-without-hadoop /opt/spark
RUN sudo rm spark-3.5.3-bin-without-hadoop.tgz
RUN sudo chmod -R 744 /opt/spark/

COPY ./config/* /opt/hadoop/etc/hadoop
RUN sudo chmod -R 744 /opt/hadoop/

RUN sudo mkdir /opt/jars
COPY ./jars/UA124/* /opt/jars
RUN sudo chmod -R 744 /opt/jars/

RUN sudo mkdir -p /opt/hadoop/data/nameNode
RUN sudo chmod -R 744 /opt/hadoop/

RUN sudo chown -R hadoop /opt/jars
RUN sudo chown -R hadoop /opt/hadoop/

ENV SPARK_DIST_CLASSPATH='/opt/hadoop/etc/hadoop:/opt/hadoop/share/hadoop/common/lib/*:/opt/hadoop/share/hadoop/common/*:/opt/hadoop/share/hadoop/hdfs:/opt/hadoop/share/hadoop/hdfs/lib/*:/opt/hadoop/share/hadoop/hdfs/*:/opt/hadoop/share/hadoop/mapreduce/*:/opt/hadoop/share/hadoop/yarn:/opt/hadoop/share/hadoop/yarn/lib/*:/opt/hadoop/share/hadoop/yarn/*'
