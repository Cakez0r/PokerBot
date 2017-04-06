import sys
import tensorflow as tf
import numpy as np

def load_file(filename):
  with open(filename) as f:
    lines = f.readlines()
    return [np.fromstring(s, sep=' ') for s in lines]

FEATURE_COUNT = 15
CLASS_COUNT = 169

HIDDEN_1_COUNT = round((FEATURE_COUNT + CLASS_COUNT) * 0.66)
HIDDEN_2_COUNT = HIDDEN_1_COUNT

def get_batch(data, batch_index, batch_size, slice_start, slice_end):
  start = batch_index * batch_size
  end = (batch_index + 1) * batch_size
  batch = []
  slice_size = slice_end - slice_start
  
  for i in range(start, end):
    idx = slice_start + (i % slice_size)
    batch.append(data[idx])
  return batch

def main(_):
  input_layer = tf.placeholder(tf.float32, [None, FEATURE_COUNT], 'input')
  weights_1 = tf.get_variable('weights_1', shape=[FEATURE_COUNT, HIDDEN_1_COUNT], initializer=tf.contrib.layers.xavier_initializer()) # tf.Variable(tf.zeros([FEATURE_COUNT, HIDDEN_COUNT]))
  biases_1 = tf.Variable(tf.constant(0.1, shape=[HIDDEN_1_COUNT]))
  hidden_layer_1 = tf.nn.relu(tf.matmul(input_layer, weights_1) + biases_1)
  weights_2 = tf.get_variable('weights_2', shape=[HIDDEN_1_COUNT, HIDDEN_2_COUNT], initializer=tf.contrib.layers.xavier_initializer()) # tf.Variable(tf.zeros([HIDDEN_COUNT, CLASS_COUNT]))
  biases_2 = tf.Variable(tf.constant(0.1, shape=[HIDDEN_2_COUNT]))
  hidden_layer_2 = tf.nn.relu(tf.matmul(hidden_layer_1, weights_2) + biases_2)
  weights_3 = tf.get_variable('weights_3', shape=[HIDDEN_2_COUNT, CLASS_COUNT], initializer=tf.contrib.layers.xavier_initializer())
  biases_3 = tf.Variable(tf.constant(0.1, shape=[CLASS_COUNT]))
  output_layer = tf.matmul(hidden_layer_2, weights_3) + biases_3
  softmax_output = tf.nn.softmax(output_layer)

  output_actual = tf.placeholder(tf.float32, [None, CLASS_COUNT], 'output_actual')
  cross_entropy = tf.reduce_mean(tf.nn.softmax_cross_entropy_with_logits(labels=output_actual, logits=output_layer))
  train_step = tf.train.AdamOptimizer(1e-4).minimize(cross_entropy)

  session = tf.InteractiveSession()
  tf.global_variables_initializer().run()
  saver = tf.train.Saver()
  
  data = load_file('data')
  labels = load_file('labels')

  batch_size = 100
  batches_per_epoch = len(data) / batch_size
  num_epochs = 5
  iterations = batches_per_epoch * num_epochs

  for i in range(round(iterations)):
    session.run(train_step, { input_layer: get_batch(data, i, 100, 0, len(data) - 10000), output_actual: get_batch(labels, i, 100, 0, len(data) - 10000) })

  correct_prediction = tf.equal(tf.argmax(output_layer, 1), tf.argmax(output_actual, 1))
  accuracy = tf.reduce_mean(tf.cast(correct_prediction, tf.float32))
  print(session.run(accuracy, { input_layer: get_batch(data, 0, 10000, len(data) - 10000, len(data)), output_actual: get_batch(labels, 0, 10000, len(data) - 10000, len(data)) }))

  saver.save(session, './preflop')

  print(session.run(accuracy, { input_layer: data, output_actual: labels }))

if __name__ == '__main__':
  main(sys.argv[1:])