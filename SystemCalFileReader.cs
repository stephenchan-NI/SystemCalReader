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
        private List<double> frequencyList = new List<double>();
        private List<double> powerList = new List<double>();
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
                        data.loss = Convert.ToDouble(portCalData.ReadElementContentAsString());
                        calData.Add(data);
                        frequencyList.Add(data.frequency);
                        powerList.Add(data.power);
                    }
                }
            }
        }

        //Currently only interpolates for frequency. 
        public double getPathloss(double freq, double pow)
        {
            double pathLoss = interpolateFreq(freq);
            return pathLoss;
        }

        private void getInterpolationPoints()
        {

        }

        public double interpolateFreq(double freq)
        {
            //The frequency list should be sorted, but we sort it anyway to be sure.
            double[] interpolationArray = frequencyList.ToArray();
            Array.Sort(interpolationArray);
            int interpolationIndex = Array.BinarySearch(interpolationArray, freq);
            //BinarySearch returns positive index if exact match is found. If so, return path loss at index. 
            if (interpolationIndex > 0)
            {
                return calData[interpolationIndex].loss;
            }
            //Binary search returns Bitwise complement of the index of the first value larger than Frequency. 
            else
            {
                double interpolatedLoss = interpolate(~interpolationIndex, freq);
                return interpolatedLoss;
            }
        }

        private double interpolate(int index, double freq)
        {
            double slope = (calData[index].loss - calData[index - 1].loss) / (calData[index].frequency - calData[index - 1].frequency);
            double interpolatedLoss = slope * (freq - calData[index - 1].frequency) + calData[index - 1].loss;
            return interpolatedLoss;
        }

    }
}
