using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Poker
{
    public class StaticData : IStaticData
    {
        public IReadOnlyList<double> AveragePredictionVector { get; private set; }

        public IReadOnlyList<Tuple<HandClass, double>> EvenWeights { get; private set; }

        public IReadOnlyList<HandClass> AllPossibleHands { get; private set; }

        public StaticData(string dataPath)
        {
            var possibleHands = JsonConvert.DeserializeObject<List<HandClass>>(File.ReadAllText(dataPath + "/ordering.json"));
            AllPossibleHands = possibleHands.AsReadOnly();

            var evenWeights = new List<Tuple<HandClass, double>>();
            foreach (var poss in AllPossibleHands)
            {
                evenWeights.Add(Tuple.Create(poss, (double)poss.Expand().Count / 1326)); // 1326 == (52 c 2)
            }

            EvenWeights = evenWeights;

            var averagePredictionVector = JsonConvert.DeserializeObject<IReadOnlyList<double>>(File.ReadAllText(dataPath + "/average.json"));
            AveragePredictionVector = averagePredictionVector;
        }
    }
}