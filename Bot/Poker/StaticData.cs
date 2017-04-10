﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Poker
{
    public class StaticData : IStaticData
    {
        public IReadOnlyList<double> AveragePreflopPredictionVector { get; private set; }

        public IReadOnlyList<double> AverageFlopPredictionVector { get; private set; }

        public IReadOnlyList<Tuple<HandClass, double>> EvenWeights { get; private set; }

        public IReadOnlyList<HandClass> AllPossibleHands { get; private set; }

        public IReadOnlyDictionary<HandClass, IReadOnlyList<Card[]>> HandClassExpansions { get; private set; }

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

            var averagePreflopPredictionVector = JsonConvert.DeserializeObject<IReadOnlyList<double>>(File.ReadAllText(dataPath + "/preflop/average.json"));
            AveragePreflopPredictionVector = averagePreflopPredictionVector;

            var averageFlopPredictionVector = JsonConvert.DeserializeObject<IReadOnlyList<double>>(File.ReadAllText(dataPath + "/flop/average.json"));
            AverageFlopPredictionVector = averageFlopPredictionVector;

            Dictionary<HandClass, IReadOnlyList<Card[]>> expansions = new Dictionary<HandClass, IReadOnlyList<Card[]>>();
            foreach (var cls in AllPossibleHands)
            {
                expansions.Add(cls, cls.Expand());
            }
            HandClassExpansions = expansions;
        }
    }
}