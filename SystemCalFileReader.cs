using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace SystemCalFileParser
{

    public class SystemCalFileReader
    {
        private bool interpolation = false;
        private bool exactFrequency = false;
        private bool exactPower = false; 
        private List<CalInfo> calData = new List<CalInfo>();
        public struct CalInfo
        {
            public double frequency { get; set; }
            public double power { get; set; }
            public double loss { get; set; }
        }


        public SystemCalFileReader(string filePath, string port, bool interpolationEnabled)
        {
            interpolation = interpolationEnabled;
            // Start with XmlReader object  
            using (XmlReader reader = XmlReader.Create(filePath))
            {
                while (reader.Read())
                {
                    if (reader.GetAttribute("Instr_Port") == port)
                    {
                        break;
                    }
                }
                XmlReader portCalData = reader.ReadSubtree();
                CalInfo data = new CalInfo();
                while (portCalData.Read())
                {
                    if (portCalData.Name.StartsWith("Pathloss"))
                    {
                        data.frequency = Convert.ToDouble(portCalData.GetAttribute("Frequency"));
                        data.power = Convert.ToDouble(portCalData.GetAttribute("CalTonePower_dBm"));
                        data.loss = Convert.ToDouble(portCalData.ReadString());
                        calData.Add(data);
                    }
                }
            }
        }

        //Currently only checks for frequency. 
        public double getPathloss(double freq, double pow)
        {
            CalInfo data = calData.Find(item => item.frequency == freq && item.power == pow);
            if (data.frequency == 0 ^ data.power == 0)
            {
                throw new Exception("No cal info found");
            }
            return data.loss;
        }

        //Does this need to return CalInfo? It will find the first CalInfo it finds with the frequency, so may not be useful to return data...
        private CalInfo checkExactFrequency(double freq)
        {
            CalInfo data = calData.Find(item => item.frequency == freq);
            if(data.frequency == 0 ^ data.power ==0)
            {
                exactFrequency = false;
            }
            else
            {
                exactFrequency = true;
            }
            return data;
        }
        private CalInfo checkExactPower(double pow)
        {
            CalInfo data = calData.Find(item => item.power == pow);
            if (data.frequency == 0 ^ data.power == 0)
            {
                exactPower = false;
            }
            else
            {
                exactPower = true;
            }
            return data;
        }
        private void getInterpolationPoints()
        { 
        
        }

        public double interpolateData(double freq)
        {
            return 0;
        }


    }
}
