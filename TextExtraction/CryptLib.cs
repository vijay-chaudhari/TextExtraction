using System.Data;
using System.Security.Cryptography;
using System.Text;

namespace TextExtraction
{
    public class CryptLib
    {
        /*****************************************************************
      * CrossPlatform CryptLib
      * 
      * <p>
      * This cross platform CryptLib uses AES 256 for encryption. This library can
      * be used for encryptoion and de-cryption of string on iOS, Android and Windows
      * platform.<br/>
      * Features: <br/>
      * 1. 256 bit AES encryption
      * 2. Random IV generation. 
      * 3. Provision for SHA256 hashing of key. 
      * </p>
      * 
      * @since 1.0
      * @author navneet
      *****************************************************************/
        private static byte[] m_DesIV = { 25, 199, 107, 22, 59, 48, 31, 33, 42, 80, 98, 65, 15, 10, 36, 48 };
        private static string m_SoftwarePassword = "e372b89e20a0f9eefc7b22d219331fa=";//PSPL

        /***
         * Encryption mode enumeration
         */
        private enum EncryptMode { ENCRYPT, DECRYPT };

        static readonly char[] CharacterMatrixForRandomIVStringGeneration = {
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M',
            'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z',
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
            'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-', '_'
        };

        /**
         * This function generates random string of the given input length.
         * 
         * @param _plainText
         *            Plain text to be encrypted
         * @param _key
         *            Encryption Key. You'll have to use the same key for decryption
         * @return returns encrypted (cipher) text
         */
        internal static string GenerateRandomIV(int length)
        {
            char[] _iv = new char[length];
            byte[] randomBytes = new byte[length];

            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(randomBytes); //Fills an array of bytes with a cryptographically strong sequence of random values. 
            }

            for (int i = 0; i < _iv.Length; i++)
            {
                int ptr = randomBytes[i] % CharacterMatrixForRandomIVStringGeneration.Length;
                _iv[i] = CharacterMatrixForRandomIVStringGeneration[ptr];
            }

            return new string(_iv);
        }


        /**
         * 
         * @param _inputText
         *            Text to be encrypted or decrypted
         * @param _encryptionKey
         *            Encryption key to used for encryption / decryption
         * @param _mode
         *            specify the mode encryption / decryption
         * @param _initVector
         * 			  initialization vector
         * @return encrypted or decrypted string based on the mode
        */
        private static string encryptDecrypt(string _inputText, string _encryptionKey, EncryptMode _mode, string _initVector)
        {

            string _out = "";
            UTF8Encoding _enc = new UTF8Encoding();
            RijndaelManaged _rcipher = new RijndaelManaged();
            _rcipher.Mode = CipherMode.CBC;
            _rcipher.Padding = PaddingMode.PKCS7;
            _rcipher.KeySize = 256;
            _rcipher.BlockSize = 128;
            byte[] _key = new byte[32];
            byte[] _iv = new byte[_rcipher.BlockSize / 8]; //128 bit / 8 = 16 bytes
            byte[] _pwd = Encoding.UTF8.GetBytes(_encryptionKey);

            int len = _pwd.Length;
            if (len > _key.Length)
            {
                len = _key.Length;
            }
            int ivLenth = m_DesIV.Length;
            if (ivLenth > _iv.Length)
            {
                ivLenth = _iv.Length;
            }

            Array.Copy(_pwd, _key, len);
            Array.Copy(m_DesIV, _iv, ivLenth);
            _rcipher.Key = _key;
            _rcipher.IV = _iv;
            if (_mode.Equals(EncryptMode.ENCRYPT))
            {
                //encrypt
                byte[] plainText = _rcipher.CreateEncryptor().TransformFinalBlock(_enc.GetBytes(_inputText), 0, _inputText.Length);
                _out = Convert.ToBase64String(plainText);
            }
            if (_mode.Equals(EncryptMode.DECRYPT))
            {
                //decrypt
                byte[] plainText = _rcipher.CreateDecryptor().TransformFinalBlock(Convert.FromBase64String(_inputText), 0, Convert.FromBase64String(_inputText).Length);
                _out = _enc.GetString(plainText);
            }
            _rcipher.Dispose();
            return _out;// return encrypted/decrypted string
        }

        /**
         * This function encrypts the plain text to cipher text using the key
         * provided. You'll have to use the same key for decryption
         * 
         * @param _plainText
         *            Plain text to be encrypted
         * @param _key
         *            Encryption Key. You'll have to use the same key for decryption
         * @return returns encrypted (cipher) text
         */
        public static string Encrypt(string data/*, string outFile, string userName*/)
        {
            string iv = GenerateRandomIV(16); //16 bytes = 128 bits
            string patientPassword = getHashSha256("CHILDPASS", 31); //32 bytes = 256 bits
            string encryptData = encryptDecrypt(data, patientPassword, EncryptMode.ENCRYPT, iv);
            return encryptData;
            //string encryptUserName = encryptDecrypt(userName, patientPassword, EncryptMode.ENCRYPT, iv);
            //DataColumn dtcDC = new DataColumn("DC", encryptData.GetType());
            //DataColumn dtcUN = new DataColumn("UN", typeof(string));
            //DataColumn dtcUP = new DataColumn("SP", typeof(string));

            //DataTable LDataTable = new DataTable("DT");
            //DataSet LDataSet = new DataSet("DS");

            //LDataTable.Columns.Add(dtcDC);
            //LDataTable.Columns.Add(dtcUN);
            //LDataTable.Columns.Add(dtcUP);
            //LDataSet.Tables.Add(LDataTable);
            //DataRow LDataRow1 = LDataSet.Tables["DT"].NewRow();
            //LDataRow1["DC"] = encryptData;
            //LDataRow1["UN"] = encryptUserName;
            //LDataRow1["SP"] = m_SoftwarePassword;
            //LDataSet.Tables["DT"].Rows.Add(LDataRow1);
            //// The Final Data Set is ready.
            //LDataSet.WriteXml(outFile, System.Data.XmlWriteMode.WriteSchema);
        }

        /***
         * This funtion decrypts the encrypted text to plain text using the key
         * provided. You'll have to use the same key which you used during
         * encryprtion
         * 
         * @param _encryptedText
         *            Encrypted/Cipher text to be decrypted
         * @param _key
         *            Encryption key which you used during encryption
         * @return encrypted value
         */

        public static string decrypt(string encryptedData)
        {

            string iv = CryptLib.GenerateRandomIV(16); //16 bytes = 128 bits
            string patientPassword = CryptLib.getHashSha256("CHILDPASS", 31); //32 bytes = 256 bits
            string decryptData = encryptDecrypt(encryptedData, patientPassword, EncryptMode.DECRYPT, iv);
            return decryptData;
        }

        /***
         * This function decrypts the encrypted text to plain text using the key
         * provided. You'll have to use the same key which you used during
         * encryption
         * 
         * @param _encryptedText
         *            Encrypted/Cipher text to be decrypted
         * @param _key
         *            Encryption key which you used during encryption
         */
        public static string getHashSha256(string text, int length)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            SHA256Managed hashstring = new SHA256Managed();
            byte[] hash = hashstring.ComputeHash(bytes);
            string hashString = string.Empty;
            foreach (byte x in hash)
            {
                hashString += String.Format("{0:x2}", x); //covert to hex string
            }
            if (length > hashString.Length)
                return hashString;
            else
                return hashString.Substring(0, length);
        }
    }
}
