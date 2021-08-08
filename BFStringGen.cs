using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AceBruteforcer
{
    class BFStringGen
    {
        private static char[] fCharList =
        {
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
            'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't',
            'u', 'v', 'w', 'x', 'y', 'z',

             'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J',
            'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
            'U', 'V', 'W', 'X', 'Y', 'Z',

            '1', '2', '3', '4', '5', '6', '7', '8', '9'
        };

        /// <summary>
        /// Start Brute Force.
        /// </summary>
        /// <param name="length">Words length.</param>
        public static void StartBruteForce(int length, ref List<byte[]> hashWordList)
        {
            StringBuilder sb = new StringBuilder(length);
            char currentChar = fCharList[0];

            for (int i = 1; i <= length; i++)
            {
                sb.Append(currentChar);
            }

            ChangeCharacters(0, sb, length, ref hashWordList);
        }

        private static StringBuilder ChangeCharacters(int pos, StringBuilder sb, int length, ref List<byte[]> hashWordList)
        {
            for (int i = 0; i <= fCharList.Length - 1; i++)
            {
                //sb.setCharAt(pos, fCharList[i]);

                sb.Replace(sb[pos], fCharList[i], pos, 1);

                if (pos == length - 1)
                {
                    // Write the Brute Force generated word.
                    string generatedWord = sb.ToString();

                   // Console.WriteLine("Generated word: " + sb.ToString());


                    using (MD5 md5Hash = MD5.Create())
                    {

                        hashWordList.Add(md5Hash.ComputeHash(Encoding.UTF8.GetBytes(generatedWord)));

                    }
                }
                else
                {
                    ChangeCharacters(pos + 1, sb, length,ref hashWordList);
                }
            }

            return sb;
        }


    }
}
