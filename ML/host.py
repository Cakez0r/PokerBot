import falcon
import json
import pokerai as pai
from waitress import serve
 
class PokerResource:
    def on_post(self, req, resp):
        text = req.stream.read().decode('utf-8')
        resp.body = pai.evaluate(json.loads(text))
 
api = falcon.API()
api.add_route('/poker', PokerResource())

serve(api, host='127.0.0.1', port=25012)