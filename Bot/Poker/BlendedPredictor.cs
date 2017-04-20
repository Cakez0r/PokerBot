using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poker
{
    public class BlendedPredictor : IHandPredictor
    {
        public double BlendFactor { get; set; }

        private IHandPredictor m_a;
        private IHandPredictor m_b;

        public BlendedPredictor(IHandPredictor a, IHandPredictor b)
        {
            m_a = a ?? throw new ArgumentNullException(nameof(a));
            m_b = b ?? throw new ArgumentNullException(nameof(b));
        }

        public IReadOnlyList<Tuple<HandClass, double>> Predict(Game game, IPlayer player)
        {
            var a = m_a.Predict(game, player);
            var b = m_b.Predict(game, player);
            var bd = b.ToDictionary(t => t.Item1, t => t.Item2);

            List<Tuple<HandClass, double>> c = new List<Tuple<HandClass, double>>(a.Count);
            for (int i = 0; i < a.Count; i++)
            {
                double delta = bd[a[i].Item1] - a[i].Item2;
                c.Add(Tuple.Create(a[i].Item1, a[i].Item2 + (delta * BlendFactor)));
            }

            c.Sort((x, y) => y.Item2.CompareTo(x.Item2));

            return c;
        }
    }
}