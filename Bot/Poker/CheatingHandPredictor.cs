using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poker
{
    public class CheatingHandPredictor : IHandPredictor
    {
        private IStaticData m_staticData;

        public CheatingHandPredictor(IStaticData staticData)
        {
            m_staticData = staticData ?? throw new ArgumentNullException(nameof(staticData));
        }

        public IReadOnlyList<Tuple<HandClass, double>> Predict(Game game, IPlayer player)
        {
            HandClass playerClass = HandClass.FromCards(player.Hole[0], player.Hole[1]);

            return m_staticData.AllPossibleHands.Select(c => Tuple.Create(c, c == playerClass ? 1.0 : 0.0)).ToList();
        }
    }
}