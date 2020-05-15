using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiffWave

{
    public class UlawCodec
    {
        // Cached table for aLaw and uLaw convertion (16K * 2 bytes each)
        static private byte[] _uLawCompTableCached;

        /// <summary>
        /// Encodes input audio sample from float [-1.0, 1.0] to U-law byte
        /// </summary>
        /// <param name="audio_in"></param>
        /// <returns></returns>
        public static byte[] Encode(float[] audio_in)
        {
            short[] data = new short[audio_in.Length];
            for (int i = 0; i < audio_in.Length; i++)
            {
                data[i] = (short)(audio_in[i] * 32767);
            }
            return ConvertLinear2ULaw(data, data.Length);
        }


        /// <summary>
        /// This routine converts from 16 bit linear to ULaw by direct access to the conversion table. 
        /// </summary> 
        /// <param name="data">Array of 16 bit linear samples.
        /// <param name="size">Size of the data in the array. 
        /// <returns>New buffer of 8 bit ULaw samples.</returns>
        static internal byte[] ConvertLinear2ULaw(short[] data, int size)
        {
            byte[] newData = new byte[size];
            _uLawCompTableCached = _uLawCompTableCached == null ? CalcLinear2ULawTable() : _uLawCompTableCached;

            for (int i = 0; i < size; i++)
            {
                unchecked
                {
                    // Extend the sign bit for the sample that is constructed from two bytes
                    newData[i] = _uLawCompTableCached[(ushort)data[i] >> 2];
                }
            }
            return newData;
        }

        /// <summary>
        /// This routine converts from linear to ULaw.
        ///
        /// Craig Reese: IDA/Supercomputing Research Center 
        /// Joe Campbell: Department of Defense
        /// 29 September 1989 
        /// 
        /// References:
        /// 1) CCITT Recommendation G.711  (very difficult to follow) 
        /// 2) "A New Digital Technique for Implementation of Any
        ///     Continuous PCM Companding Law," Villeret, Michel,
        ///     et al. 1973 IEEE Int. Conf. on Communications, Vol 1,
        ///     1973, pg. 11.12-11.17 
        /// 3) MIL-STD-188-113,"Interoperability and Performance Standards
        ///     for Analog-to_Digital Conversion Techniques," 
        ///     17 February 1987 
        /// </summary>
        /// <returns>New buffer of 8 bit ULaw samples</returns> 
        static private byte[] CalcLinear2ULawTable()
        {
            /*const*/
            bool ZEROTRAP = false;      // turn off the trap as per the MIL-STD 
            const byte uBIAS = 0x84;              // define the add-in bias for 16 bit samples
            const int uCLIP = 32635;

            byte[] table = new byte[((int)UInt16.MaxValue + 1) >> 2];

            for (int i = 0; i < UInt16.MaxValue; i += 4)
            {
                short data = unchecked((short)i);

                int sample;
                int sign, exponent, mantissa;
                byte ULawbyte;

                unchecked
                {
                    // Extend the sign bit for the sample that is constructed from two bytes
                    sample = (int)((data >> 2) << 2);

                    // Get the sample into sign-magnitude.
                    sign = (sample >> 8) & 0x80;          // set aside the sign 
                    if (sign != 0)
                    {
                        sample = -sample;
                    }
                    if (sample > uCLIP) sample = uCLIP;   // clip the magnitude

                    // Convert from 16 bit linear to ULaw. 
                    sample = sample + uBIAS;
                    exponent = (int)exp_lut_linear2ulaw[(sample >> 7) & 0xFF];
                    mantissa = (int)((sample >> (exponent + 3)) & 0x0F);

                    ULawbyte = (byte)(~(sign | (exponent << 4) | mantissa));
                }

                if (ZEROTRAP)
                {
                    if (ULawbyte == 0) ULawbyte = 0x02; // optional CCITT trap
                }

                table[i >> 2] = ULawbyte;
            }

            return table;
        }

        static private int[] exp_lut_linear2ulaw = new int[256]
        {
            0,0,1,1,2,2,2,2,3,3,3,3,3,3,3,3,
            4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,
            5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,
            5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,
            6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,
            6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,
            6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,
            6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,
            7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
            7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
            7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
            7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
            7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
            7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
            7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
            7,7,7,7,7,7,7,7 ,7 ,7 ,7 ,7 ,7 ,7,7,7
        };

    }
}
