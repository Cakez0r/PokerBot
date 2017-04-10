import falcon
import json
import pokerai_oo as pai
from waitress import serve
 
class PokerResource:
    def __init__(self, feature_count, class_count, save_file, name):
        self.network = pai.PokerAi(feature_count, class_count, save_file, name)

    def on_post(self, req, resp):
        text = req.stream.read().decode('utf-8')
        resp.body = self.network.evaluate(json.loads(text))
 
api = falcon.API()
print('load preflop')
api.add_route('/preflop', PokerResource(15, 169, '../datafiles/preflop/preflop', 'preflop'))
print('load flop')
api.add_route('/flop', PokerResource(41, 169, '../datafiles/flop/flop', 'flop'))

serve(api, host='127.0.0.1', port=25012)