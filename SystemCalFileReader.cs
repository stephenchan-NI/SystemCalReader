using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml;

namespace SystemCalFileParser
{

    public class SystemCalFileReader
    {
        private bool interpolationEnabled = false;
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


        public SystemCalFileReader(string filePath, string port, bool interpolate)
        {
            try
            {
                interpolationEnabled = interpolate;
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
                            data.power = Convert.ToDouble(portCalData.GetAttribute("Power"));
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
            double pathLoss = interpolation(freq, pow);
            return pathLoss;
        }

        private void getInterpolationPoints()
        {

        }

        public double interpolation(double freq, double power)
        {
            //The frequency list should be sorted, but we sort it anyway to be sure.
            //powerOffsetIndex is the number of powers in the calibration subtracted by one. We assume that the calibration uses a consistent number of powers...
            double[] interpolationArray = frequencyList.ToArray();
            double[] powerArray = powerList.ToArray();
            Array.Sort(interpolationArray);
            Array.Sort(powerArray);
            int interpolationIndex = Array.BinarySearch(interpolationArray, freq);
            int powerOffsetIndex = Array.BinarySearch(powerArray, power);
            double pathLoss = 0;

            //BinarySearch returns positive index if exact match is found. If so, return path loss at index. 
            if (interpolationIndex >= 0)
            {
                //Now check if exact power is found. If greater than 1, returns index of element found
                if (powerOffsetIndex >= 0)
                {
                    interpolationIndex = getFirstFrequencyIndex(interpolationIndex, powerOffsetIndex);
                    return calData[interpolationIndex].loss;
                }
                //If exact power is not found, interpolate power
                else
                {
                    //TODO - If ~powerOffsetIndex = 1, this means that the requested power is lower than any power contained in the list. If so, return error?
                    //TODO - If ~powerOffsetIndex = powerArray size, this means requested power is higher than any power contained in the list. 
                    powerOffsetIndex = ~powerOffsetIndex;
                    interpolationIndex = getFirstFrequencyIndex(interpolationIndex, powerOffsetIndex);
                    pathLoss = interpolatePower(interpolationIndex, power);
                    return pathLoss;
                }

            }
            //If interpolationIndex < 0, no exact frequency match was found. 
            else
            {
                interpolationIndex = ~interpolationIndex;
                //If exact power is found
                if (powerOffsetIndex >= 0)
                {
                    interpolationIndex = getFirstFrequencyIndex(interpolationIndex, powerOffsetIndex);
                    double interpolatedLoss = interpolateFrequency(interpolationIndex, freq, powerOffsetIndex);
                    return interpolatedLoss;
                }
                //Neither exact frequency or exact power is found
                else
                {
                    //Get the index of the first power that is higher than the requested power
                    powerOffsetIndex = ~powerOffsetIndex;
                    //First, we calculate the interpolated power path loss at the upper frequency
                    interpolationIndex = getFirstFrequencyIndex(interpolationIndex, powerOffsetIndex);
                    double interpolatedLoss_f0 = interpolatePower(interpolationIndex, power);
                    //Then, we calculate the interpolated power path loss at the lower frequency
                    //TODO - Add error checking for requested frequency/power out of range of calibration file
                    double interpolatedLoss_f1 = interpolatePower(interpolationIndex - powerArray.Count(), power);

                    //Then, we interpolate the two path loss between the two frequencies above
                    double interpolatedLoss = calData[interpolationIndex].loss - (calData[interpolationIndex].frequency - freq)*(interpolatedLoss_f0 - interpolatedLoss_f1) / (calData[interpolationIndex].frequency - calData[interpolationIndex - powerArray.Count()].frequency);
                    return interpolatedLoss;
                }
            }
        }

        private double interpolateFrequency(int index, double freq, int powerCount)
        {
            try
            {
                //File format dictates that at a given power, the next frequency is powerCount indexes away
                double slope = (calData[index].loss - calData[index - powerCount].loss) / (calData[index].frequency - calData[index - powerCount].frequency);
                double interpolatedLoss = slope * (freq - calData[index - powerCount].frequency) + calData[index - powerCount].loss;
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

        private double interpolatePower(int index, double pow)
        {
            try
            {
                double slope = (calData[index].loss - calData[index - 1].loss) / (calData[index].power - calData[index - 1].power);
                double interpolatedLoss = slope * (pow - calData[index - 1].power) + calData[index - 1].loss;
                return interpolatedLoss;
            }
            catch (ArgumentOutOfRangeException ex)
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
        private int getFirstFrequencyIndex(int startingIndex, int powerOffsetIndex)
        {

            double freq = calData[startingIndex].frequency;
            //Back up index in case binary search returned middle of duplicate index. 
            if (startingIndex > powerList.Count)
            {
                startingIndex -= powerList.Count;
            }
            else
            {
                startingIndex = 0;
            }
            //interpolationIndex now points towards first frequency element
            //We then increment until we find the first index 
            while (calData[startingIndex].frequency != freq)
            {
                startingIndex++;
            }
            //Now that we have the first index, we increment it by the calculated power offset
            startingIndex += powerOffsetIndex;
            return startingIndex;

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
