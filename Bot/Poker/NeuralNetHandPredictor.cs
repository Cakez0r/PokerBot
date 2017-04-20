using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;

namespace Poker
{
    public class NeuralNetHandPredictor : IHandPredictor
    {
        private RestClient m_rc = new RestClient("http://localhost:25012");

        private IStaticData m_staticData;

        public NeuralNetHandPredictor(IStaticData staticData)
        {
            m_staticData = staticData ?? throw new ArgumentNullException(nameof(staticData));
        }

        public IReadOnlyList<Tuple<HandClass, double>> Predict(Game game, IPlayer player)
        {
            var vector = game.Log.MakeVector(player.ToString(), game.State);
            IReadOnlyList<double> avg = m_staticData.AveragePredictionVectors[game.State];
            for (int i = 0; i < vector.Length; i++)
            {
                vector[i] -= avg[i];
            }

            var request = new RestRequest(game.State.ToString().ToLower(), Method.POST);
            request.AddJsonBody(vector);
            var response = m_rc.Post(request);
            double[] probabilities = JsonConvert.DeserializeObject<double[]>(response.Content);

            List<Tuple<HandClass, double>> handResult = new List<Tuple<HandClass, double>>(169);

            for (int i = 0; i < probabilities.Length; i++)
            {
                handResult.Add(Tuple.Create(m_staticData.AllPossibleHands[i], probabilities[i]));
            }

            handResult.Sort((x, y) => y.Item2.CompareTo(x.Item2));

            return handResult;
        }
    }
}