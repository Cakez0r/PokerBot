import sys
import tensorflow as tf
import numpy as np
import json

class PokerAi:
    def __init__(self, feature_count, class_count, save_file, name):
        self.graph = tf.Graph()
        with self.graph.as_default():
            with tf.variable_scope(name):
                self.feature_count = feature_count
                self.class_count = class_count

                self.hidden_1_count = round((feature_count + class_count) * 0.5)
                self.hidden_2_count = self.hidden_1_count

                self.input_layer = tf.placeholder(tf.float32, [None, feature_count], 'input')

                self.weights_1 = tf.Variable(tf.zeros([self.feature_count, self.hidden_1_count]))
                self.biases_1 = tf.Variable(tf.constant(0.1, shape=[self.hidden_1_count]))
                self.hidden_layer_1 = tf.nn.relu(tf.matmul(self.input_layer, self.weights_1) + self.biases_1)
                self.hidden_layer_1_drop = tf.nn.dropout(self.hidden_layer_1, 1)

                self.weights_2 = tf.Variable(tf.zeros([self.hidden_1_count, self.class_count]))
                self.biases_2 = tf.Variable(tf.constant(0.1, shape=[self.class_count]))
                #self.hidden_layer_2 = tf.nn.relu(tf.matmul(self.hidden_layer_1, self.weights_2) + self.biases_2)

                #self.weights_3 = tf.get_variable('weights_3', shape=[self.hidden_2_count, self.hidden_2_count], initializer=tf.contrib.layers.xavier_initializer()) # tf.Variable(tf.zeros([HIDDEN_COUNT, CLASS_COUNT]))
                #self.biases_3 = tf.Variable(tf.constant(0.1, shape=[self.hidden_2_count]))
                #self.hidden_layer_3 = tf.nn.relu(tf.matmul(self.hidden_layer_2, self.weights_3) + self.biases_3)

                #self.weights_4 = tf.get_variable('weights_4', shape=[self.hidden_2_count, self.hidden_2_count], initializer=tf.contrib.layers.xavier_initializer()) # tf.Variable(tf.zeros([HIDDEN_COUNT, CLASS_COUNT]))
                #self.biases_4 = tf.Variable(tf.constant(0.1, shape=[self.hidden_2_count]))
                #self.hidden_layer_4 = tf.nn.relu(tf.matmul(self.hidden_layer_3, self.weights_4) + self.biases_4)

                #self.weights_5 = tf.get_variable('weights_5', shape=[self.hidden_2_count, self.class_count], initializer=tf.contrib.layers.xavier_initializer())
                #self.biases_5 = tf.Variable(tf.constant(0.1, shape=[self.class_count]))
                self.output_layer = tf.matmul(self.hidden_layer_1_drop, self.weights_2) + self.biases_2

                self.softmax_output = tf.nn.softmax(self.output_layer)

                self.session = tf.Session(graph=self.graph)
                self.saver = tf.train.Saver()
                self.saver.restore(self.session, save_file)

    def evaluate(self, vec):
        return json.dumps(self.session.run(self.softmax_output, { self.input_layer: [vec] })[0].tolist())