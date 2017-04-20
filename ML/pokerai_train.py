import sys
import tensorflow as tf
import numpy as np
import datetime as dt
import os

def load_file(filename):
  if os.path.isfile(filename + '.npy'):
    return np.load(filename + '.npy')
  
  with open(filename) as f:
    lines = f.readlines()
    ary = [np.fromstring(s, sep=' ') for s in lines]
    np.save(filename + '.npy', ary)
    return ary

def load_file_slice(filename, slice):
  with open(filename) as f:
    lines = f.readlines()
    return [np.fromstring(s, sep=' ')[:slice] for s in lines]

def get_batch(data, batch_index, batch_size, slice_start, slice_end):
  start = batch_index * batch_size
  end = (batch_index + 1) * batch_size
  batch = []
  slice_size = slice_end - slice_start
  
  for i in range(start, end):
    idx = slice_start + (i % slice_size)
    batch.append(data[idx])
  return batch

def main(args):
  datafile = args[0]
  labelfile = args[1]
  outfolder = args[2]
  network_name = args[3]

  data = load_file(datafile)
  labels = load_file(labelfile)

  batch_size = 1000
  test_set = 100000
  batches_per_epoch = (len(data) - test_set) / batch_size
  num_epochs = 80
  rate_decay = batches_per_epoch * 20
  iterations = batches_per_epoch * num_epochs
  update_interval = 1000
  highest_accuracy = 0
  start_time = dt.datetime.now()

  FEATURE_COUNT = len(data[0])
  CLASS_COUNT = len(labels[0])
  HIDDEN_COUNT = round((FEATURE_COUNT + CLASS_COUNT) * 0.8)

  with tf.variable_scope(network_name):
    keep_prob = tf.placeholder(tf.float32)
    input_layer = tf.placeholder(tf.float32, [None, FEATURE_COUNT], 'input')

    weights_1 = tf.get_variable('weights_1', shape=[FEATURE_COUNT, HIDDEN_COUNT], initializer=tf.contrib.layers.xavier_initializer())
    biases_1 = tf.get_variable('biases_1', shape=[HIDDEN_COUNT], initializer=tf.contrib.layers.xavier_initializer())
    hidden_layer_1 = tf.nn.relu(tf.matmul(input_layer, weights_1) + biases_1)
    hidden_layer_1_drop = tf.nn.dropout(hidden_layer_1, keep_prob)

    weights_2 = tf.get_variable('weights_2', shape=[HIDDEN_COUNT, HIDDEN_COUNT], initializer=tf.contrib.layers.xavier_initializer())
    biases_2 = tf.get_variable('biases_2', shape=[HIDDEN_COUNT], initializer=tf.contrib.layers.xavier_initializer())
    hidden_layer_2 = tf.nn.relu(tf.matmul(hidden_layer_1_drop, weights_2) + biases_2)
    hidden_layer_2_drop = tf.nn.dropout(hidden_layer_2, keep_prob)

    weights_3 = tf.get_variable('weights_3', shape=[HIDDEN_COUNT, CLASS_COUNT], initializer=tf.contrib.layers.xavier_initializer())
    biases_3 = tf.get_variable('biases_3', shape=[CLASS_COUNT], initializer=tf.contrib.layers.xavier_initializer())
    output_layer = tf.matmul(hidden_layer_2_drop, weights_3) + biases_3

    softmax_output = tf.nn.softmax(output_layer)

    global_step = tf.Variable(0, trainable=False)

    output_actual = tf.placeholder(tf.float32, [None, CLASS_COUNT], 'output_actual')
    cross_entropy = tf.reduce_mean(tf.nn.softmax_cross_entropy_with_logits(labels=output_actual, logits=output_layer))
    learning_rate = tf.train.exponential_decay(1e-2, global_step, rate_decay, 0.1, staircase=True)
    train_step = tf.train.AdamOptimizer(learning_rate).minimize(cross_entropy, global_step=global_step)

    session = tf.InteractiveSession()
    tf.global_variables_initializer().run()
    saver = tf.train.Saver()
    
    correct_prediction = tf.equal(tf.argmax(output_layer, 1), tf.argmax(output_actual, 1))
    accuracy = tf.reduce_mean(tf.cast(correct_prediction, tf.float32))

    for i in range(round(iterations)):
      session.run(train_step, { input_layer: get_batch(data, i, batch_size, 0, len(data) - test_set), output_actual: get_batch(labels, i, batch_size, 0, len(data) - test_set), keep_prob: 0.5 })
      if i % update_interval == 1:
        delta = dt.datetime.now() - start_time
        delta = delta / i
        finishes_in = delta * (iterations - i)
        progress = round((i / iterations) * 100)
        unknown_accuracy = session.run(accuracy, { input_layer: get_batch(data, 0, test_set, len(data) - test_set, len(data)), output_actual: get_batch(labels, 0, test_set, len(data) - test_set, len(data)), keep_prob: 1.0 })
        unknown_accuracy = unknown_accuracy * 100
        # print('Known accuracy: ' + str(session.run(accuracy, { input_layer: get_batch(data, 0, 10000, 0, len(data)), output_actual: get_batch(labels, 0, 10000, 0, len(data)), keep_prob: 1.0 })))

        #if unknown_accuracy > highest_accuracy and i > batches_per_epoch * 20:
        #  print('Saving new high...')
        #  saver.save(session, state + '/' + state)
        #  highest_accuracy = unknown_accuracy

        print('[' + str(progress) + '%] Accuracy: ' + str(unknown_accuracy) + '%  -  Best: ' + str(highest_accuracy) + '%  -  Time remaining: ' + str(finishes_in))

    saver.save(session, outfolder + '/network')
    print('Finished')
    print('Accuracy: ' + str(unknown_accuracy))

    f = open(outfolder + '/accuracy', 'w')
    f.write(str(highest_accuracy))
    f.flush()

if __name__ == '__main__':
  main(sys.argv[1:])