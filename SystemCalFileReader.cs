using System;
using System.Collections.Generic;
using System.Net;
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
            try
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
                            if (!powerList.Contains(data.power))
                            {
                                powerList.Add(data.power);
                            }
                        }
                    }
                }
            }
            //Error occurs if specified port is not contained in calibration file
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException("Specified port not contained in cal file", ex);
            }
        }

        //Currently only interpolates for frequency. 
        public double getPathloss(double freq, double pow)
        {
            double pathLoss = interpolateFreq(freq, pow);
            return pathLoss;
        }

        private void getInterpolationPoints()
        {

        }

        public double interpolateFreq(double freq, double power)
        {
            //The frequency list should be sorted, but we sort it anyway to be sure.
            //powerOffsetIndex is the number of powers in the calibration subtracted by one. We assume that the calibration uses a consistent number of powers...
            double[] interpolationArray = frequencyList.ToArray();
            double[] powerArray = powerList.ToArray();
            Array.Sort(interpolationArray);
            Array.Sort(powerArray);
            int interpolationIndex = Array.BinarySearch(interpolationArray, freq);
            int powerOffsetIndex = Array.BinarySearch(powerArray, power);


            //BinarySearch returns positive index if exact match is found. If so, return path loss at index. 
            if (interpolationIndex >= 0)
            {
                //Back up index in case binary search returned middle of duplicate index. Then, we look for the first instance of our frequency.                   
                interpolationIndex -= powerOffsetIndex;
                while (calData[interpolationIndex].frequency != freq)
                {
                    interpolationIndex++;
                }
                //interpolationIndex now points towards first frequency element.
                //Now check if exact power is found. If greater than 1, returns index of element found
                if (powerOffsetIndex >=  0)
                {
                    interpolationIndex += powerOffsetIndex;
                    return calData[interpolationIndex].loss;
                }
                //If exact power is not found, interpolate power
                else
                {
                    return calData[interpolationIndex].loss;
                }

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
            try
            {
                double slope = (calData[index].loss - calData[index - 1].loss) / (calData[index].frequency - calData[index - 1].frequency);
                double interpolatedLoss = slope * (freq - calData[index - 1].frequency) + calData[index - 1].loss;
                return interpolatedLoss;
            }
            catch(ArgumentOutOfRangeException ex)
            {
                throw new ArgumentOutOfRangeException("Requested frequency out of bounds of calibration file", ex);
                return 0;
            }
            catch
            {
                throw;
                return 0;
            }
        }

        private bool checkExactFrequency(double freq)
        {
            return frequencyList.Contains(freq);
        }
        private bool checkExactPower(double pow)
        {
            return powerList.Contains(pow); ;
        }

    }
}
