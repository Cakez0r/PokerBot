import os
import falcon
import json
import pokerai_oo as pai
from waitress import serve
 
os.environ['TF_CPP_MIN_LOG_LEVEL'] = '3'

class PokerResource:
    def __init__(self, feature_count, class_count, save_file, name):
        self.network = pai.PokerAi(feature_count, class_count, save_file, name)

    def on_post(self, req, resp):
        text = req.stream.read().decode('utf-8')
        resp.body = self.network.evaluate(json.loads(text))

PREFLOP_FEATURES = 15
POSTFLOP_FEATURES = 26

api = falcon.API()
print('load preflop')
api.add_route('/preflop', PokerResource(PREFLOP_FEATURES, 169, '../datafiles/preflop/preflop', 'preflop'))
print('load flop')
api.add_route('/flop', PokerResource(PREFLOP_FEATURES + POSTFLOP_FEATURES, 169, '../datafiles/flop/flop', 'flop'))
print('load turn')
api.add_route('/turn', PokerResource(PREFLOP_FEATURES + (POSTFLOP_FEATURES*2), 169, '../datafiles/turn/turn', 'turn'))
print('load river')
api.add_route('/river', PokerResource(PREFLOP_FEATURES + (POSTFLOP_FEATURES*3), 169, '../datafiles/river/river', 'river'))

serve(api, host='127.0.0.1', port=25012)