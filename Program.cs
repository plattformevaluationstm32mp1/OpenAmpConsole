using System;
using System.IO;

class Program
{
    public struct CanMessage
    {
        public long messageNumber = 0;
        public long timeOffset100ns = 0;

        public string type = "";

        public string direction = "";

        public uint receiverId = 0;
        public uint messageId = 0;
        public uint senderId = 0;
        public uint canId = 0;

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
        long timeOffset1ms = canMessage.timeOffset100ns / 10000;
        Console.WriteLine($"{canMessage.messageNumber,7:D} {timeOffset1ms/1000,9:D}.{timeOffset1ms%1000:D3} {canMessage.type}     {canMessage.canId:X4} {canMessage.direction} {canMessage.frameLength} {BitConverter.ToString(canMessage.data, 0, canMessage.frameLength).Replace("-", " ")}");
    }

    static void Main(string[] args)
    {
        string fileToRead;
        if(args.Length==0)
        {
            fileToRead = "/dev/ttyRPMSG1";
        }
        else
        {
            fileToRead = args[0];
        }

        try
        {
            // Open the device file
            using (FileStream fileStream = new FileStream(fileToRead, FileMode.Open))
            using (StreamReader streamReader = new StreamReader(fileStream))
            {
                // Read lines from the device file
                while (true)
                {
                    string line = streamReader.ReadLine();
                    if (line == null)
                        break;

                        try
                        {
                            // create a new CAN message
                            CanMessage canMessage = new CanMessage();

                            //split everything in seperated fields with space as separator
                            char[] separators = { ' ' };
                            string[] fields = line.Split(separators, StringSplitOptions.RemoveEmptyEntries);

                            if(fields.Length > 0)
                            {
                                //field 0: message number
                                canMessage.messageNumber = long.Parse(fields[0]);

                                //field 1: time offset
                                string timeOffsetString = fields[1].Replace(".", "");
                                canMessage.timeOffset100ns = long.Parse(timeOffsetString) * 10000;

                                //field 2: type
                                canMessage.type = fields[2];

                                //field 3: CanId
                                canMessage.canId = uint.Parse(fields[3], System.Globalization.NumberStyles.HexNumber);
                                canMessage.receiverId = canMessage.canId & 0x000F;          //receiver id: bit 3..0
                                canMessage.messageId = (canMessage.canId & 0x01F0) >> 4;    //message id: bit 9..4
                                canMessage.senderId = (canMessage.canId & 0x0300) >> 9;     //message id: bit 11..10 

                                //field 4: direction
                                canMessage.direction = fields[4];

                                //field 5: data length
                                canMessage.frameLength = int.Parse(fields[5]);

                                //field 6: can data
                                for (int i = 0; i < canMessage.frameLength; i++)
                                {
                                    canMessage.data[i] = byte.Parse(fields[6 + i], System.Globalization.NumberStyles.HexNumber);
                                }

                                PrintRawCanData(canMessage.canId, canMessage);
                            }
                    }
                    catch (FormatException ex)
                    {
                        Console.Error.WriteLine("Format error of the received data: " + ex.Message);
                    }
                }
            }
        }
        catch (IOException ex)
        {
            Console.Error.WriteLine("Failed to read data from RPMsg UART device file: " + ex.Message);
        }
    }
}
