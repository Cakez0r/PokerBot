using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Poker
{
    public class StaticData : IStaticData
    {
        public IReadOnlyDictionary<HandState, IReadOnlyList<double>> AveragePredictionVectors { get; private set; }

        public IReadOnlyList<Tuple<HandClass, double>> EvenWeights { get; private set; }

        public IReadOnlyList<HandClass> AllPossibleHands { get; private set; }

        public IReadOnlyDictionary<HandClass, IReadOnlyList<Card[]>> HandClassExpansions { get; private set; }

        public IReadOnlyList<double> BetRamp { get; private set; }

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

            var averagePredictionVectors = new Dictionary<HandState, IReadOnlyList<double>>();
            var averagePreflopPredictionVector = JsonConvert.DeserializeObject<IReadOnlyList<double>>(File.ReadAllText(dataPath + "/preflop/preflop_average.json"));
            averagePredictionVectors[HandState.Preflop] = averagePreflopPredictionVector;

            var averageFlopPredictionVector = JsonConvert.DeserializeObject<IReadOnlyList<double>>(File.ReadAllText(dataPath + "/flop/flop_average.json"));
            averagePredictionVectors[HandState.Flop] = averageFlopPredictionVector;

            var averageTurnPredictionVector = JsonConvert.DeserializeObject<IReadOnlyList<double>>(File.ReadAllText(dataPath + "/turn/turn_average.json"));
            averagePredictionVectors[HandState.Turn] = averageTurnPredictionVector;

            var averageRiverPredictionVector = JsonConvert.DeserializeObject<IReadOnlyList<double>>(File.ReadAllText(dataPath + "/river/river_average.json"));
            averagePredictionVectors[HandState.River] = averageRiverPredictionVector;

            AveragePredictionVectors = averagePredictionVectors;

            Dictionary<HandClass, IReadOnlyList<Card[]>> expansions = new Dictionary<HandClass, IReadOnlyList<Card[]>>();
            foreach (var cls in AllPossibleHands)
            {
                expansions.Add(cls, cls.Expand());
            }
            HandClassExpansions = expansions;

            double[] betRamp = JsonConvert.DeserializeObject<double[]>(File.ReadAllText(dataPath + "/ramp.json"));
            BetRamp = betRamp;
        }
    }
}