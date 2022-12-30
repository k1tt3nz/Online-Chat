using System;
using System.Linq;
using System.Text;

namespace FeistelNet
{
    class FeistelNetClass
    {
        private const int BLOCK_SIZE = 8;           //8 байт = 64 бита
        private const int ROUNDS = 16;
        private const UInt64 startKey = 239780456;  //начальный 64-битовый ключ

        public static UInt64[] Encrypt(String text)
        {
            UInt64[] blocks = _getBlocks(text);
            UInt64[] resultEncrypted = _feistelEncrypt(blocks);

            String cipherText = Encoding.UTF8.GetString(resultEncrypted.SelectMany(r => BitConverter.GetBytes(r).Reverse()).ToArray());
            return resultEncrypted;
        }

        public static String Decrypt(String text)
        {
            UInt64[] blocks = _getBlocks(text);
            UInt64[] resultDecrypted = _feistelDecrypt(blocks);

            String plainText = Encoding.UTF8.GetString(resultDecrypted.SelectMany(r => BitConverter.GetBytes(r).Reverse()).ToArray());
            return plainText;
        }

        /// <summary>
        /// Шифрация блоков данных итеративно
        /// </summary>
        /// <param name="plainText"></param>
        /// <returns></returns>
        private static UInt64[] _feistelEncrypt(UInt64[] plainText)
        {
            var encryptedBlocks = new UInt64[plainText.Count()];
            for (int i = 0; i < plainText.Count(); i++)
            {
                encryptedBlocks[i] = _encryptBlock(plainText[i]);
            }

            return encryptedBlocks;
        }

        /// <summary>
        /// Дешифраця блоков данных итеративно
        /// </summary>
        /// <param name="encryptedText"></param>
        /// <returns></returns>
        private static UInt64[] _feistelDecrypt(UInt64[] encryptedText)
        {
            var decryptedBlocks = new UInt64[encryptedText.Count()];
            for (int i = 0; i < encryptedText.Count(); i++)
            {
                decryptedBlocks[i] = _decryptBlock(encryptedText[i]);
            }

            return decryptedBlocks;
        }

        /// <summary>
        /// Шифрация блока данных
        /// </summary>
        /// <param name="originalBlock"></param>
        /// <returns></returns>
        private static UInt64 _encryptBlock(UInt64 originalBlock)
        {
            byte[] bytes = _getBytes(originalBlock);
            //два блока длиною по 32 бита
            UInt32 leftPart = _toUInt32(bytes.Take(4).ToArray());
            UInt32 rightPart = _toUInt32(bytes.Skip(4).Take(4).ToArray());

            //каждый из блоков еще разбиваем на два подблока длиною в 16 бит
            UInt16 onePart = _toUInt16(_getBytes(leftPart).Take(2).ToArray());
            UInt16 twoPart = _toUInt16(_getBytes(leftPart).Skip(2).Take(2).ToArray());
            UInt16 threePart = _toUInt16(_getBytes(rightPart).Take(2).ToArray());
            UInt16 fourPart = _toUInt16(_getBytes(rightPart).Skip(2).Take(2).ToArray());

            //прогонка раундов
            for (int i = 0; i < ROUNDS; i++)
            {
                UInt32 roundKey = _genRoundKey(startKey, i * 8);
                UInt16 f = _roundF(onePart, roundKey);

                UInt16 tmp1Part = threePart;
                UInt16 tmp2Part = fourPart;

                onePart = (UInt16)(onePart ^ (f ^ threePart));
                twoPart = (UInt16)(twoPart ^ (f ^ fourPart));

                threePart = onePart;
                fourPart = twoPart;

                onePart = tmp1Part; 
                twoPart = tmp2Part;

            }

            //сливаем блоки по 16 бит в два блока по 32
            UInt32 left32 = _unionUint16Blocks(onePart, twoPart);

            //затем сливаем блоки по 32 в единый блок 64 бита
            UInt32 right32 = _unionUint16Blocks(threePart, fourPart);

            return _unionUint32Blocks(left32, right32);
        }

        /// <summary>
        /// Объединение блоков длиною 16 бит в один блок 32 бита
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        private static UInt32 _unionUint16Blocks(UInt16 left, UInt16 right)
        {
            UInt16[] parts = { left, right };
            var unitedBytes = new byte[4];
            int index = 0; int length = unitedBytes.Length;
            int number = 2;
            for (int i = 0; i < length; i++)
            {
                if (i % length == 2)
                {
                    ++index; number = 2;
                }
                unitedBytes[i] = BitConverter.GetBytes(parts[index])[--number];
            }

            return _toUInt32(unitedBytes);
        }

        /// <summary>
        /// Объединение блоков длиною 32 бит в один блок 64 бита
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        private static UInt64 _unionUint32Blocks(UInt32 left, UInt32 right)
        {
            UInt32[] parts = { left, right };
            var unitedBytes = new byte[8];
            int index = 0; int length = unitedBytes.Length;
            int number = 4;
            for (int i = 0; i < length; i++)
            {
                if (i % length == 4)
                {
                    ++index; number = 4;
                }
                unitedBytes[i] = BitConverter.GetBytes(parts[index])[--number];
            }
            return _toUInt64(unitedBytes);
        }

        /// <summary>
        /// Дешифрация блока данных
        /// </summary>
        /// <param name="originalBlock"></param>
        /// <returns></returns>
        private static UInt64 _decryptBlock(UInt64 originalBlock)
        {
            byte[] bytes = _getBytes(originalBlock);
            UInt32 leftPart = _toUInt32(bytes.Take(4).ToArray());
            UInt32 rightPart = _toUInt32(bytes.Skip(4).Take(4).ToArray());

            //каждый из блоков еще разбиваем на два подблока длиною в 16 бит
            UInt16 onePart = _toUInt16(_getBytes(leftPart).Take(2).ToArray());
            UInt16 twoPart = _toUInt16(_getBytes(leftPart).Skip(2).Take(2).ToArray());
            UInt16 threePart = _toUInt16(_getBytes(rightPart).Take(2).ToArray());
            UInt16 fourPart = _toUInt16(_getBytes(rightPart).Skip(2).Take(2).ToArray());

            //прогонка раундов
            for (int i = ROUNDS - 1; i >= 0; i--)
            {
                UInt32 roundKey = _genRoundKey(startKey, i * 8);
                UInt16 f = _roundF(onePart, roundKey);
                if (i > 0)
                {
                    UInt16 tmp = onePart;
                    onePart = (UInt16)(fourPart ^ f);
                    fourPart = (UInt16)(threePart ^ f);
                    threePart = (UInt16)(twoPart ^ f);
                    twoPart = tmp;
                }
                else
                {
                    threePart ^= f;
                    twoPart ^= f;
                    fourPart ^= f;
                }
            }
            UInt32 left32 = _unionUint16Blocks(onePart, twoPart);
            UInt32 right32 = _unionUint16Blocks(threePart, fourPart);

            return _unionUint32Blocks(left32, right32);
        }

        /// <summary>
        /// Дополнение массива байт, если их число не кратное
        /// </summary>
        private static void _addUpFullBlock(ref byte[] bytes, string text)
        {
            byte[] temp = new byte[text.Length + (8 - text.Length % 8)];
            Array.Copy(bytes, temp, bytes.Length);
            bytes = temp;
        }

        /// <summary>
        /// Разбитие строки на блоки данных длиною в 64 бит
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static UInt64[] _getBlocks(string text)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);

            //дополнение массива байт, если их число не кратное
            if (text.Length % BLOCK_SIZE != 0)
                _addUpFullBlock(ref bytes, text);

            var blocksCount = (int)Math.Ceiling(bytes.Count() / (double)BLOCK_SIZE);
            var blocks = new UInt64[blocksCount];
            for (int i = 0; i < blocksCount; i++)
                blocks[i] = _toUInt64(bytes.Skip(i * BLOCK_SIZE).Take(BLOCK_SIZE).ToArray());

            return blocks;
        }

        /// <summary>
        /// Набор байт переводим в 64-битное число
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private static UInt64 _toUInt64(byte[] bytes)
        {
            UInt64 result = 0;
            for (int i = 0; i < 8; i++)
            {
                result += (UInt64)Math.Pow(256, 7 - i) * bytes[i];
            }

            return result;
        }

        /// <summary>
        /// Набор байт переводим в 32-битное число
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private static UInt32 _toUInt32(byte[] bytes)
        {
            UInt32 result = 0;
            for (int i = 0; i < 4; i++)
            {
                result += (UInt32)Math.Pow(256, 3 - i) * bytes[i];
            }

            return result;
        }

        /// <summary>
        /// Набор байт переводим в 16-битное число
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        private static UInt16 _toUInt16(byte[] bytes)
        {
            UInt16 result = 0;
            result += (ushort)(bytes[0] * 256 + bytes[1]);

            return result;
        }

        /// <summary>
        /// Воздействующая функция
        /// </summary>
        /// <param name="originalBlock"></param>
        /// <param name="roundKey"></param>
        /// <returns></returns>
        private static UInt16 _roundF(UInt16 originalBlock, UInt32 roundKey)
        {
            UInt16 first = _rol16(originalBlock, 9);
            UInt16 second = (UInt16)~(_genRoundKey(roundKey, 11) & originalBlock);

            return (UInt16)(first ^ second);
        }

        /// <summary>
        /// Процедура для генерации раундового ключа рзмера UInt32
        /// </summary>
        /// <param name="key"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        private static UInt32 _genRoundKey(UInt32 key, int n)
        {
            UInt32 t1, t2;
            n = n % (sizeof(UInt32) * 8);
            t1 = key >> n;
            t2 = key << (sizeof(UInt32) * 8 - n);

            return t1 | t2;
        }

        /// <summary>
        /// Процедура для генерации раундового ключа рзмера UInt64
        /// </summary>
        /// <param name="key"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        private static UInt32 _genRoundKey(UInt64 key, int n)
        {
            UInt64 t1, t2;
            n = n % (sizeof(UInt64) * 8);
            t1 = key >> n;
            t2 = key << (sizeof(UInt64) * 8 - n);

            return (UInt32)(t1 | t2);                     //возвращаем половину ключа
        }

        /// <summary>
        /// Процедура для генерации раундового ключа рзмера UInt16
        /// </summary>
        /// <param name="key"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        private static UInt16 _genRoundKey(UInt16 a, int n)
        {
            UInt16 t1, t2;
            n = n % (sizeof(UInt16) * 8);                 // нормализуем n
            t1 = (UInt16)(a >> n);                        // двигаем а вправо на n бит, теряя младшие биты
            t2 = (UInt16)(a << (sizeof(UInt16) * 8 - n)); // перегоняем младшие биты в старшие

            return (UInt16)(t1 | t2);                     // объединяем старшие и младшие биты
        }


        private static UInt16 _rol16(UInt32 a, int n)
        {
            UInt16 t1, t2;
            n = n % (sizeof(UInt16) * 8);                   // нормализуем n
            t1 = (UInt16)(a << n);                          // двигаем а вправо на n бит, теряя младшие биты
            t2 = (UInt16)(a >> (sizeof(UInt16) * 8 - n));   // перегоняем младшие биты в старшие

            return (UInt16)(t1 | t2);                       // объединяем старшие и младшие биты
        }

        /// <summary>
        /// Число переводим в байты
        /// </summary>
        /// <param name="originalBlock"></param>
        /// <returns></returns>
        private static byte[] _getBytes(UInt64 originalBlock)
        {
            return BitConverter.GetBytes(originalBlock).Reverse().ToArray();
        }

        /// <summary>
        /// Число переводим в байты
        /// </summary>
        /// <param name="originalBlock"></param>
        /// <returns></returns>
        private static byte[] _getBytes(UInt32 originalBlock)
        {
            return BitConverter.GetBytes(originalBlock).Reverse().ToArray();
        }

    }
}