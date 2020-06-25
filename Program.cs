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
            string filePath = "C:\\Users\\admin\\Documents\\AnokiWave\\SystemCalFileParser\\SystemCalFileParser\\supporting files\\OTA_System_SA_Calibration.xml";
            string calPort = "rf0/port1";
            double frequency = 24125000000;
            double calTone = -1;
            double pathLoss;

            SystemCalFileReader calReader = new SystemCalFileReader(filePath, calPort, false);
            pathLoss = calReader.getPathloss(frequency, calTone);

            Console.WriteLine(pathLoss);


            Console.ReadKey();
        }
    }
}