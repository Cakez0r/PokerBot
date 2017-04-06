import os
import sys
import tensorflow as tf
import numpy as np
import json

os.environ['TF_CPP_MIN_LOG_LEVEL'] = '3'

FEATURE_COUNT = 15
CLASS_COUNT = 169

HIDDEN_1_COUNT = round((FEATURE_COUNT + CLASS_COUNT) * 0.66)
HIDDEN_2_COUNT = HIDDEN_1_COUNT

input_layer = tf.placeholder(tf.float32, [None, FEATURE_COUNT], 'input')
weights_1 = tf.get_variable('weights_1', shape=[FEATURE_COUNT, HIDDEN_1_COUNT], initializer=tf.contrib.layers.xavier_initializer())
biases_1 = tf.Variable(tf.constant(0.1, shape=[HIDDEN_1_COUNT]))
hidden_layer_1 = tf.nn.relu(tf.matmul(input_layer, weights_1) + biases_1)
weights_2 = tf.get_variable('weights_2', shape=[HIDDEN_1_COUNT, HIDDEN_2_COUNT], initializer=tf.contrib.layers.xavier_initializer())
biases_2 = tf.Variable(tf.constant(0.1, shape=[HIDDEN_2_COUNT]))
hidden_layer_2 = tf.nn.relu(tf.matmul(hidden_layer_1, weights_2) + biases_2)
weights_3 = tf.get_variable('weights_3', shape=[HIDDEN_2_COUNT, CLASS_COUNT], initializer=tf.contrib.layers.xavier_initializer())
biases_3 = tf.Variable(tf.constant(0.1, shape=[CLASS_COUNT]))
output_layer = tf.matmul(hidden_layer_2, weights_3) + biases_3
softmax_output = tf.nn.softmax(output_layer)

session = tf.InteractiveSession()
saver = tf.train.Saver()
saver.restore(session, '../datafiles/preflop')

def evaluate(vec):
  return json.dumps(session.run(softmax_output, { input_layer: [vec] })[0].tolist())

def main(args):
  print(evaluate(np.fromiter(args, np.float)))

if __name__ == '__main__':
  main(sys.argv[1:])