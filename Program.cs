using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace CanTimestampExample
{
    class Program
    {
    public struct CanMessage
    {
        public uint receiverId = 0;
        public uint messageId = 0;
        public uint senderId = 0;
        public uint canId = 0;
        public long canTimeStamp100ns = 0;      //software can timestamp. Set by the interrupt handler when the message was received.
        public long systemTimeStamp100ns = 0;   //timestamp of the operating system when the packet was readet out of the receive queue.
        public int frameLength = 0;
        public byte[] data = new byte[64];

        public CanMessage() { }
    }

        /// <summary>
        /// For debug purposes: print the whole received can data 
        ///</summary>
        static private void PrintRawCanData(uint canId, CanMessage canMessage)
        {
            Span<byte> data = new Span<byte>(canMessage.data, 0, canMessage.frameLength);
            string type = "EFF";// : "SFF";

            Console.WriteLine($" ({canMessage.canTimeStamp100ns})   can1  {canId:X}  [{canMessage.frameLength}]  {BitConverter.ToString(canMessage.data)}");
            //Console.WriteLine($" ({canMessage.systemTimeStamp100ns})   can1  {canId:X}  [{canMessage.frameLength}]  {BitConverter.ToString(canMessage.data)}");
        }

        static void Main(string[] args)
        {
            // Start the candump process
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "candump";
            startInfo.Arguments = "-ta can1"; //this is the software timestamp
            //startInfo.Arguments = "-H -ta can0"; //this is the hardware timestamp
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            Process candumpProcess = Process.Start(startInfo);

            // Parse the output of the candump process
            using (StreamReader reader = candumpProcess.StandardOutput)
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                   // Console.WriteLine(line);
                    CanMessage canData = new CanMessage();
                    canData.systemTimeStamp100ns = DateTimeOffset.UtcNow.Ticks;

                    //split everythin in seperated fields
                    char[] separators = { ' ' };
                    string[] fields = line.Split(separators, StringSplitOptions.RemoveEmptyEntries);

                    //parse the can timestamp
                    string timeStampString = fields[0].Replace("(", "").Replace(")", "").Replace(".", "");

                    long canTimestamp1us = long.Parse(timeStampString);
                    long unixEpoch100ns = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
                    long canTimestamp100ns = 10 * canTimestamp1us;
                    canData.canTimeStamp100ns = canTimestamp100ns + unixEpoch100ns;

                    //parse the canId
                    canData.canId = uint.Parse(fields[2], System.Globalization.NumberStyles.HexNumber);
                    canData.receiverId = canData.canId & 0x000F;          //receiver id: bit 3..0
                    canData.messageId = (canData.canId & 0x01F0) >> 4;    //message id: bit 9..4
                    canData.senderId = (canData.canId & 0x0300) >> 9;     //message id: bit 11..10 

                    //parse data length
                    canData.frameLength = int.Parse(fields[3].Trim('[', ']'));

                    //parse the data
                    for (int i = 0; i < canData.frameLength; i++)
                    {
                        canData.data[i] = byte.Parse(fields[4 + i], System.Globalization.NumberStyles.HexNumber);
                    }

                    PrintRawCanData(canData.canId, canData);
                }
            }

            // Wait for the candump process to exit
            candumpProcess.WaitForExit();
        }
    }
}
