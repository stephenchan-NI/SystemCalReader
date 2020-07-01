using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using SystemCalFileParser;

namespace CalDemo

{
    class Program
    {
        static void Main(string[] args)
        {
            string filePath = "..\\..\\..\\supporting files\\AnokiWave_SA_Pathloss.xml";
            string calPort = "rf1/port0";
            double frequency = 24100000000;
            double calTone = -5;
            double pathLoss;

            SystemCalFileReader calReader = new SystemCalFileReader(filePath, calPort, false);
            pathLoss = calReader.getPathloss(frequency, calTone);

            Console.WriteLine(pathLoss);


            Console.ReadKey();
        }
    }
}