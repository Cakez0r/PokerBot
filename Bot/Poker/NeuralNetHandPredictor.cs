using Newtonsoft.Json;
using NLog;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public IReadOnlyList<Tuple<HandClass, double>> Estimate(IReadOnlyList<double> predictionVector)
        {
            //StringBuilder args = new StringBuilder();
            //args.Append("pokerai_run.py ");
            //foreach (var d in predictionVector)
            //{
            //    args.Append(d.ToString());
            //    args.Append(' ');
            //}

            //var info = new ProcessStartInfo("python")
            //{
            //    Arguments = args.ToString(),
            //    RedirectStandardOutput = true,
            //    UseShellExecute = false,
            //    StandardOutputEncoding = Encoding.ASCII
            //};
            //Process p = new Process();
            //p.StartInfo = info;
            //p.Start();

            //p.WaitForExit();

            //string result = p.StandardOutput.ReadToEnd();

            var request = new RestRequest("poker", Method.POST);
            request.AddJsonBody(predictionVector);
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
