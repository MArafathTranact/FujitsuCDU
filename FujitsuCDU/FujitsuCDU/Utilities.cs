using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FujitsuCDU
{
    public class Utilities
    {
        private readonly Logger logger = new Logger();
        public string ByteToHexaEZCash(byte[] bytes, int sizeRead)
        {
            StringBuilder sb = new StringBuilder();

            string response = SplitBytes(bytes, 16);
            return response;
        }

        public string ByteToHexaCDC(byte[] bytes, int sizeRead)
        {
            StringBuilder sb = new StringBuilder();

            string response = SplitBytesCDU(bytes, 16);
            return response;
        }

        public string SplitBytesCDU(byte[] inputBytes, int bytesToSplit)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte[] copySlice in inputBytes.Slices(bytesToSplit))
            {
                // do something with each slice
                StringBuilder sbtemp = new StringBuilder();
                for (int i = 0; i < copySlice.Length; i++)
                {
                    sbtemp.Append(copySlice[i].ToString("X2") + " ");
                }
                string rawdata = ConvertHex(sbtemp.ToString().Replace(" ", "")).Replace("\u000e", ".").Replace("\0\t", "").Replace("\u001c", ".").Replace("\n", ".").Replace("", ".").Replace("", ".").Replace("\u0015", ".")
                    .Replace("", "").Replace("\u001b", ".").Replace("\f", ".").Replace("\u000f", ".").Replace("\r", ".").Replace("\u0002", ".").Replace("\u001a", ".").Replace("ÿ", ".").Replace("\0", ".");//.Replace("\u0004", "").Replace("\u0001", "").Replace("\u0005", "");
                LogEvents($"  {sbtemp,-48} |{rawdata,-16}|"); // Gives empty white spaces to the right for specified length
                //sb.Append(sbtemp.ToString().Replace(" ", ""));
                sb.Append(sbtemp.ToString().Replace(" ", ""));

            }

            return sb.ToString();

        }

        public string SplitBytes(byte[] inputBytes, int bytesToSplit)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte[] copySlice in inputBytes.Slices(bytesToSplit))
            {
                // do something with each slice
                StringBuilder sbtemp = new StringBuilder();
                for (int i = 0; i < copySlice.Length; i++)
                {
                    sbtemp.Append(copySlice[i].ToString("X2") + " ");
                }
                string rawdata = ConvertHex(sbtemp.ToString().Replace(" ", "")).Replace("\u000e", ".").Replace("\0\t", "").Replace("\u001c", ".").Replace("\n", ".").Replace("", ".").Replace("", ".")
                    .Replace("", "").Replace("\u001b", ".").Replace("\f", ".").Replace("\u000f", ".").Replace("\r", ".").Replace("\u0002", ".").Replace("\u001a", ".").Replace("ÿ", ".").Replace("\0", ".");//.Replace("\u0004", "").Replace("\u0001", "").Replace("\u0005", "");
                LogEvents($"  {sbtemp,-48} |{rawdata,-16}|"); // Gives empty white spaces to the right for specified length
                //sb.Append(sbtemp.ToString().Replace(" ", ""));
                sb.Append(rawdata);

            }

            return sb.ToString();

        }

        private void LogEvents(string input)
        {
            logger.Log(input);
        }

        public string ConvertHex(string hexString)
        {
            try
            {
                string ascii = string.Empty;

                for (int i = 0; i < hexString.Length; i += 2)
                {
                    string hs = string.Empty;

                    hs = hexString.Substring(i, 2);
                    ulong decval = Convert.ToUInt64(hs, 16);
                    long deccc = Convert.ToInt64(hs, 16);
                    char character = Convert.ToChar(deccc);
                    ascii += character;

                }

                return ascii;
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }

            return string.Empty;
        }

        public byte[] StringToByteArray(String hex, bool checkOdd)
        {
            int NumberChars = hex.Length;
            if (checkOdd && NumberChars % 2 != 0)
                NumberChars++;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        public byte[] GetSendBytes(string input)
        {
            try
            {
                //byte[] ba = Encoding.Default.GetBytes(input);
                var hexString = input;//BitConverter.ToString(ba);
                //hexString = hexString.Replace("2D", "0A");
                //hexString = hexString.Replace("2E", "1C");
                //hexString = hexString.Replace("-", "");
                var dataarray = StringToByteArray(hexString, true);

                //SplitBytes(dataarray, 16); // Write detailed information into Log file.

                //string values = dataarray.Length.ToString("x");

                //while (values.Length < 4)
                //{
                //    values = "0" + values;

                //}
                //byte[] headerBytes = StringToByteArray(values, true);

                return dataarray;//headerBytes.Concat(dataarray).ToArray();
            }
            catch (Exception ex)
            {

                throw;
            }


        }

        public string ReplaceLastOccurrence(string Source, string Find, string Replace)
        {
            int place = Source.LastIndexOf(Find);

            if (place == -1)
                return Source;

            string result = Source.Remove(place, Find.Length).Insert(place, Replace);
            return result;
        }

        public string ReplaceFirstOccurrence(string Source, string Find, string Replace)
        {
            int place = Source.IndexOf(Find);

            if (place < 0)
                return Source;

            string result = Source.Remove(place, Find.Length).Insert(place, Replace);
            return result;
        }

        public byte[] GetSendBytesForCard(string input, bool replace)
        {
            try
            {
                byte[] ba = Encoding.Default.GetBytes(input);
                var hexString = BitConverter.ToString(ba);
                //hexString = hexString.Replace("2E", "1C");
                var hexArray = hexString.Split(new string[] { "0A" }, StringSplitOptions.None);
                hexArray[0] = hexArray[0].Replace("2E", "1C");
                hexString = string.Join("0A", hexArray);

                if (replace)
                {
                    hexString = ReplaceFirstOccurrence(hexString, "2D", "1D");
                    hexString += "-0C-1C";
                }
                else
                {
                    hexString += "-0C";
                }
                hexString = hexString.Replace("-", "");
                var dataarray = StringToByteArray(hexString, true);

                string response = SplitBytes(dataarray, 16);

                string values = dataarray.Length.ToString("x");

                while (values.Length < 4)
                {
                    values = "0" + values;

                }
                byte[] headerBytes = StringToByteArray(values, true);

                return headerBytes.Concat(dataarray).ToArray();
            }
            catch (Exception)
            {

                throw;
            }
        }

        public bool CheckBillCountStatus(string billCount)
        {
            var valid = false;
            try
            {
                if (string.IsNullOrWhiteSpace(billCount))
                {
                    return true;

                }
                switch (billCount)
                {
                    case "0":
                        valid = true;
                        break;
                    case "1":
                        return false;
                    case "2":
                        return false;
                    case "3":
                        return false;
                    case "4":
                        valid = true;
                        break;
                    case "5":
                        return false;
                    case "6":
                        return false;

                }

                return valid;
            }
            catch (Exception e)
            {
                //LogTransaction.LogEvents(e.InnerException.Message.ToString(), 3450);
                return valid;
            }

        }
    }



    public static class SplitBytes
    {
        public static T[] CopySlice<T>(this T[] source, int index, int length, bool padToLength = false)
        {
            int n = length;
            T[] slice = null;

            if (source.Length < index + length)
            {
                n = source.Length - index;
                if (padToLength)
                {
                    slice = new T[length];
                }
            }

            if (slice == null) slice = new T[n];
            Array.Copy(source, index, slice, 0, n);
            return slice;
        }

        public static IEnumerable<T[]> Slices<T>(this T[] source, int count, bool padToLength = false)
        {
            for (var i = 0; i < source.Length; i += count)
                yield return source.CopySlice(i, count, padToLength);
        }
    }
}
